using Kaffebar.Orm.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Testcontainers.PostgreSql;

namespace Kaffebar.Orm.Tests;

/// <summary>
/// Én ferdig migrert og seedet Postgres per testklasse. Testene i én klasse deler den;
/// ulike klasser får hver sin, så oppgavene som endrer data ikke ødelegger for nivå 1.
/// </summary>
/// <remarks>
/// Har du ikke en container-runtime i det hele tatt, kan du sette miljøvariabelen
/// <c>KAFFEBAR_POSTGRES</c> til en connection string mot en Postgres du har selv. Da lager
/// fixturen en egen database per testklasse der, og sletter den etterpå.
/// </remarks>
public class PostgresFixture : IAsyncLifetime
{
    /// <summary>
    /// Sett til <c>false</c> for å skjule SQL-en EF sender. Advarslene fra EF og dataene testene skriver
    /// ut vises fortsatt.
    /// </summary>
    private static readonly bool ShowSql = true;

    private PostgreSqlContainer? _container;
    private string? _externalAdminConnectionString;
    private string? _externalDatabase;

    public string ConnectionString { get; private set; } = null!;
    public string Runtime { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        var external = Environment.GetEnvironmentVariable("KAFFEBAR_POSTGRES");
        if (!string.IsNullOrWhiteSpace(external))
        {
            await CreateExternalDatabaseAsync(external);
        }
        else
        {
            var (endpoint, description) = ContainerRuntime.Detect();
            // Fullt kvalifisert navn: Podman godtar ikke alltid «postgres:18-alpine» alene.
            var builder = new PostgreSqlBuilder("docker.io/library/postgres:18-alpine")
                .WithDatabase("kaffebar")
                .WithUsername("kaffebar")
                .WithPassword("kaffebar");
            if (endpoint is not null)
                builder = builder.WithDockerEndpoint(endpoint);

            _container = builder.Build();
            await _container.StartAsync();
            ConnectionString = _container.GetConnectionString();
            Runtime = description;
        }

        await using var db = KaffebarDb.Create(ConnectionString);
        await db.Database.MigrateAsync();
        SeedData.Populate(db);
    }

    /// <summary>En ny context. Gi den <paramref name="log"/> for å se SQL-en i testresultatet.</summary>
    public KaffebarDbContext CreateContext(
        Action<string>? log = null, QueryCounter? counter = null, bool lazyLoading = false) =>
        KaffebarDb.Create(ConnectionString, log is null || ShowSql ? log : WithoutSql(log), counter, lazyLoading);

    private static Action<string> WithoutSql(Action<string> log) =>
        message => { if (!message.StartsWith("Executed DbCommand")) log(message); };

    /// <summary>
    /// Ordrens versjon, <c>xmin</c>, rett fra databasen. Fungerer før du har lagt til
    /// <c>Version</c> på <c>Order</c> i oppgave 4A.
    /// </summary>
    public async Task<uint> ReadVersionAsync(Guid orderId)
    {
        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand("SELECT xmin::text::bigint FROM orders WHERE id = @id", connection);
        command.Parameters.AddWithValue("id", orderId);
        return (uint)(long)(await command.ExecuteScalarAsync())!;
    }

    public async Task DisposeAsync()
    {
        if (_container is not null)
            await _container.DisposeAsync();

        if (_externalDatabase is not null)
        {
            NpgsqlConnection.ClearAllPools();
            await using var admin = new NpgsqlConnection(_externalAdminConnectionString);
            await admin.OpenAsync();
            await using var drop = new NpgsqlCommand($"DROP DATABASE IF EXISTS \"{_externalDatabase}\" WITH (FORCE)", admin);
            await drop.ExecuteNonQueryAsync();
        }
    }

    private async Task CreateExternalDatabaseAsync(string adminConnectionString)
    {
        _externalAdminConnectionString = adminConnectionString;
        _externalDatabase = $"kaffebar_{Guid.NewGuid():N}";

        await using (var admin = new NpgsqlConnection(adminConnectionString))
        {
            await admin.OpenAsync();
            await using var create = new NpgsqlCommand($"CREATE DATABASE \"{_externalDatabase}\"", admin);
            await create.ExecuteNonQueryAsync();
        }

        ConnectionString = new NpgsqlConnectionStringBuilder(adminConnectionString) { Database = _externalDatabase }.ConnectionString;
        Runtime = $"KAFFEBAR_POSTGRES (database {_externalDatabase})";
    }
}
