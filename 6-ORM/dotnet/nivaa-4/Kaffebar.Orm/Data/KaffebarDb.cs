using Microsoft.EntityFrameworkCore;
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
            builder
                .LogTo(logSql,
                    [DbLoggerCategory.Database.Command.Name, DbLoggerCategory.Query.Name],
                    LogLevel.Information)
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
