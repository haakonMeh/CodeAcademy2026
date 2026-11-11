using System.Globalization;
using System.Text.RegularExpressions;
using Kaffebar.Orm.Data;
using Kaffebar.Orm.Domain;
using Microsoft.EntityFrameworkCore;
using Xunit.Abstractions;

namespace Kaffebar.Orm.Tests;

/// <summary>
/// Felles utskrift for testene: overskrifter, nøkkeltall, kommandoene EF sendte og hva change
/// trackeren følger med på. Du trenger ikke å endre noe her for å løse oppgavene.
/// </summary>
internal static partial class Print
{
    private const int Width = 72;

    public static string Kr(decimal amount) => amount.ToString("0.00", CultureInfo.InvariantCulture) + " kr";

    public static void Heading(this ITestOutputHelper output, string title)
    {
        output.WriteLine("");
        output.WriteLine($"── {title} {new string('─', Math.Max(3, Width - title.Length - 4))}");
    }

    /// <summary>Navn og verdi, én per linje, med verdiene på linje.</summary>
    public static void Facts(this ITestOutputHelper output, params (string Label, object? Value)[] facts)
    {
        var width = facts.Max(f => f.Label.Length);
        foreach (var (label, value) in facts)
            output.WriteLine($"  {label.PadRight(width)}  {Format(value)}");
    }

    /// <summary>En tabell. Tall høyrejusteres, alt annet venstrejusteres.</summary>
    public static void Table(this ITestOutputHelper output, string[] headers, IEnumerable<object?[]> rows)
    {
        var cells = rows.Select(row => row.Select(Format).ToArray()).ToList();
        var numeric = Enumerable.Range(0, headers.Length)
            .Select(i => cells.Count > 0 && cells.All(row => IsNumber(row[i])))
            .ToArray();
        var widths = Enumerable.Range(0, headers.Length)
            .Select(i => cells.Select(row => row[i].Length).Append(headers[i].Length).Max())
            .ToArray();

        string Line(string[] values) => "  " + string.Join("  ", values.Select((value, i) =>
            numeric[i] ? value.PadLeft(widths[i]) : value.PadRight(widths[i]))).TrimEnd();

        output.WriteLine(Line(headers));
        foreach (var row in cells)
            output.WriteLine(Line(row));
    }

    /// <summary>En ordre med linjene sine, slik den står i databasen.</summary>
    public static void Order(this ITestOutputHelper output, Order order)
    {
        output.WriteLine($"  {order.CustomerName} ({order.Status}), TotalPrice {Kr(order.TotalPrice)}");
        output.Table(
            ["Linje", "Antall", "Drikk", "Størrelse", "Enhetspris", "Linjesum"],
            order.Lines.OrderBy(l => l.Id).Select(l => new object?[]
                { l.Id, l.Quantity, l.DrinkName, l.Size, l.UnitPrice, l.Quantity * l.UnitPrice }));
        output.Facts(("Sum av linjene", Kr(order.Lines.Sum(l => l.Quantity * l.UnitPrice))));
    }

    /// <summary>
    /// Alt <see cref="QueryCounter"/> så, gruppert på setning: SELECT med tabellene den leser,
    /// INSERT/UPDATE/DELETE med tabellen den skriver til, og kolonnene en UPDATE setter.
    /// </summary>
    public static void Commands(this ITestOutputHelper output, QueryCounter counter, string title = "Kommandoer til databasen")
    {
        output.Heading(title);
        if (counter.Commands.Count == 0)
        {
            output.WriteLine("  Ingen kommandoer.");
            return;
        }

        var statements = counter.Commands
            .SelectMany(command => Split(command.Sql).Select(sql => (Statement: Describe(sql), command)))
            .GroupBy(s => s.Statement)
            .Select(g => new object?[]
            {
                g.Key,
                g.Count(),
                g.Key.StartsWith("SELECT") ? g.Max(s => s.command.Columns) : null,
                g.Key.StartsWith("SELECT") ? g.Sum(s => s.command.Rows) : null
            });
        output.Table(["Setning", "Antall", "Kolonner", "Rader"], statements);
        output.WriteLine("");
        output.Facts(("Rundturer til databasen", counter.CommandCount), ("Rader lest", counter.RowsRead));
    }

    /// <summary>Entitetene contexten følger med på, gruppert på type og tilstand.</summary>
    public static void Tracked(this ITestOutputHelper output, DbContext db, string title = "Change trackeren")
    {
        output.Heading(title);
        var entries = db.ChangeTracker.Entries().ToList();
        if (entries.Count == 0)
        {
            output.WriteLine("  Tom. Ingen entiteter sporet.");
            return;
        }

        output.Table(["Entitet", "Tilstand", "Antall"], entries
            .GroupBy(e => (Type: e.Metadata.ClrType.Name, e.State))
            .OrderBy(g => g.Key.Type, StringComparer.Ordinal)
            .Select(g => new object?[] { g.Key.Type, g.Key.State, g.Count() }));
        output.WriteLine("");
        output.Facts(("Sporet totalt", entries.Count));
    }

    private static string Format(object? value) => value switch
    {
        null => "",
        decimal d => d.ToString("0.00", CultureInfo.InvariantCulture),
        IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
        _ => value.ToString() ?? ""
    };

    private static bool IsNumber(string value) =>
        value.Length == 0 || decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out _);

    // SaveChanges kan sende flere setninger i én kommando, adskilt med semikolon og linjeskift.
    private static IEnumerable<string> Split(string sql) =>
        StatementEnd().Split(sql).Select(s => s.Trim()).Where(s => s.Length > 0);

    private static string Describe(string sql)
    {
        var verb = sql.Split((char[])[' ', '\n', '\r', '\t'], 2)[0].ToUpperInvariant();
        switch (verb)
        {
            case "SELECT":
            case "WITH":
                var ctes = CteName().Matches(sql).Select(m => m.Groups[1].Value).ToHashSet();
                var tables = TableRead().Matches(sql)
                    .Select(m => m.Groups[1].Value)
                    .Where(t => !ctes.Contains(t))
                    .Distinct();
                return $"SELECT {string.Join(", ", tables)}";
            case "INSERT":
                return $"INSERT {Target(sql, InsertTarget())}";
            case "DELETE":
                return $"DELETE {Target(sql, DeleteTarget())}";
            case "UPDATE":
                var set = SetClause().Match(sql).Groups[1].Value;
                var columns = SetColumn().Matches(set).Select(m => m.Groups[1].Value);
                return $"UPDATE {Target(sql, UpdateTarget())} SET {string.Join(", ", columns)}";
            default:
                return verb;
        }
    }

    private static string Target(string sql, Regex regex) => regex.Match(sql).Groups[1].Value;

    [GeneratedRegex(@";\s*(?:\r?\n|$)")]
    private static partial Regex StatementEnd();

    [GeneratedRegex(@"\b(?:FROM|JOIN)\s+""?(\w+)""?", RegexOptions.IgnoreCase)]
    private static partial Regex TableRead();

    [GeneratedRegex(@"(\w+)\s+AS\s*\(", RegexOptions.IgnoreCase)]
    private static partial Regex CteName();

    [GeneratedRegex(@"^INSERT\s+INTO\s+""?(\w+)""?", RegexOptions.IgnoreCase)]
    private static partial Regex InsertTarget();

    [GeneratedRegex(@"^DELETE\s+FROM\s+""?(\w+)""?", RegexOptions.IgnoreCase)]
    private static partial Regex DeleteTarget();

    [GeneratedRegex(@"^UPDATE\s+""?(\w+)""?", RegexOptions.IgnoreCase)]
    private static partial Regex UpdateTarget();

    [GeneratedRegex(@"\bSET\s+(.*?)(?:\s+FROM\s|\s+WHERE\s|\s+RETURNING\s|$)", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex SetClause();

    [GeneratedRegex(@"(?:^|,)\s*""?(\w+)""?\s*=")]
    private static partial Regex SetColumn();
}
