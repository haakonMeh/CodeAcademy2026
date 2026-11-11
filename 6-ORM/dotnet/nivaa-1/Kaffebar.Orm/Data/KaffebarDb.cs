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

        // Forarbeid: EF sin SQL-logging, som loggeren org.hibernate.SQL i Hibernate. Kun SQL-kommandoene og
        // advarslene om spørringene, uten tidsstempel og kategori, så det er lett å lese i testresultatet.
        // EnableSensitiveDataLogging viser parameterverdiene i stedet for «?». Aldri i prod.
        if (logSql is not null)
        {
            builder
                .LogTo(
                    (id, level) => id == RelationalEventId.CommandExecuted || id == RelationalEventId.CommandError
                        || (level >= LogLevel.Warning && id.Name?.StartsWith(DbLoggerCategory.Query.Name) == true),
                    data => logSql(data.LogLevel >= LogLevel.Warning ? $"{data.LogLevel}: {data.EventIdCode}\n{data}" : $"{data}"))
                .EnableSensitiveDataLogging();
        }

        if (counter is not null)
            builder.AddInterceptors(counter);

        // Oppgave 1B: lazy loading er AV som standard i EF Core. Pakken
        // Microsoft.EntityFrameworkCore.Proxies er allerede installert.
        if (lazyLoading)
        {
            // TODO 1B: skru på lazy loading her.
        }

        return new KaffebarDbContext(builder.Options);
    }
}
