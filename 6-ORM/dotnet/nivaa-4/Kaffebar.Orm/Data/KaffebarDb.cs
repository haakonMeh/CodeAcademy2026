using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Kaffebar.Orm.Data;

/// <summary>
/// Det ene stedet der en <see cref="KaffebarDbContext"/> blir konfigurert. Ingen DI og ingen
/// hosting: det er deg, en connection string og EF Core.
/// </summary>
public static class KaffebarDb
{
    public static KaffebarDbContext Create(
        string connectionString,
        Action<string>? logSql = null,
        QueryCounter? counter = null,
        bool lazyLoading = false)
    {
        var builder = new DbContextOptionsBuilder<KaffebarDbContext>()
            .UseNpgsql(connectionString)
            .UseSnakeCaseNamingConvention();
        if (logSql is not null)
        {
            // SQL-en EF sender, og advarslene den gir om spørringene. Uten tidsstempel og
            // kategori, så det er lett å lese i testresultatet.
            builder
                .LogTo(
                    (id, level) => id == RelationalEventId.CommandExecuted || id == RelationalEventId.CommandError
                        || (level >= LogLevel.Warning && id.Name?.StartsWith(DbLoggerCategory.Query.Name) == true),
                    data => logSql(data.LogLevel >= LogLevel.Warning ? $"{data.LogLevel}: {data.EventIdCode}\n{data}" : $"{data}"))
                .EnableSensitiveDataLogging();
        }

        if (counter is not null)
            builder.AddInterceptors(counter);

        // Oppgave 1B: lazy loading er AV som standard i EF Core. Her skrur vi den på.
        if (lazyLoading)
            builder.UseLazyLoadingProxies();

        return new KaffebarDbContext(builder.Options);
    }
}
