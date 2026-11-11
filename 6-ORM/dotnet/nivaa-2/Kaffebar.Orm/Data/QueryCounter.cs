using System.Collections;
using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Kaffebar.Orm.Data;

/// <summary>
/// Teller hva EF faktisk sender til databasen, og hvor mye som kommer tilbake. EF Core har
/// ingen innebygd «QueryCountLogger», men en <see cref="DbCommandInterceptor"/> ser hver
/// eneste kommando.
/// </summary>
/// <remarks>
/// Én instans per test gir tall du kan asserte på uten å nullstille noe mellom testene.
/// Du trenger ikke å endre noe her for å løse oppgavene.
/// </remarks>
public class QueryCounter : DbCommandInterceptor
{
    private int _selectCount;
    private int _commandCount;
    private int _rowsRead;

    /// <summary>Antall spørringer som leser, altså SELECT, også når de starter med en CTE (<c>WITH</c>).</summary>
    public int SelectCount => _selectCount;

    /// <summary>Antall kommandoer totalt, også INSERT/UPDATE/DELETE.</summary>
    public int CommandCount => _commandCount;

    /// <summary>Antall rader databasen sendte tilbake, summert over alle spørringer.</summary>
    public int RowsRead => _rowsRead;

    public void Reset()
    {
        Interlocked.Exchange(ref _selectCount, 0);
        Interlocked.Exchange(ref _commandCount, 0);
        Interlocked.Exchange(ref _rowsRead, 0);
    }

    public override InterceptionResult<DbDataReader> ReaderExecuting(
        DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result)
    {
        Count(command);
        return result;
    }

    public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
        DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result,
        CancellationToken cancellationToken = default)
    {
        Count(command);
        return ValueTask.FromResult(result);
    }

    // Readeren pakkes inn, slik at vi kan telle hver Read(). Det er den eneste tellingen som
    // stemmer uansett hvordan EF leser (én spørring, split query, rå SQL).
    // Bare for spørringer: når SaveChanges sender flere INSERT/UPDATE i én batch, forventer
    // Npgsql sin egen NpgsqlDataReader tilbake, og da må vi holde fingrene av fatet.
    public override DbDataReader ReaderExecuted(
        DbCommand command, CommandExecutedEventData eventData, DbDataReader result) =>
        IsQuery(command) ? new RowCountingReader(result, this) : result;

    public override ValueTask<DbDataReader> ReaderExecutedAsync(
        DbCommand command, CommandExecutedEventData eventData, DbDataReader result,
        CancellationToken cancellationToken = default) =>
        ValueTask.FromResult(IsQuery(command) ? new RowCountingReader(result, this) : result);

    public override InterceptionResult<int> NonQueryExecuting(
        DbCommand command, CommandEventData eventData, InterceptionResult<int> result)
    {
        Count(command);
        return result;
    }

    public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
        DbCommand command, CommandEventData eventData, InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        Count(command);
        return ValueTask.FromResult(result);
    }

    public override InterceptionResult<object> ScalarExecuting(
        DbCommand command, CommandEventData eventData, InterceptionResult<object> result)
    {
        Count(command);
        return result;
    }

    public override ValueTask<InterceptionResult<object>> ScalarExecutingAsync(
        DbCommand command, CommandEventData eventData, InterceptionResult<object> result,
        CancellationToken cancellationToken = default)
    {
        Count(command);
        return ValueTask.FromResult(result);
    }

    private void Count(DbCommand command)
    {
        Interlocked.Increment(ref _commandCount);
        if (IsQuery(command))
            Interlocked.Increment(ref _selectCount);
    }

    private static bool IsQuery(DbCommand command)
    {
        var sql = command.CommandText.TrimStart();
        return sql.StartsWith("SELECT", StringComparison.OrdinalIgnoreCase)
            || sql.StartsWith("WITH", StringComparison.OrdinalIgnoreCase);
    }

    private void RowRead() => Interlocked.Increment(ref _rowsRead);

    /// <summary>Sender alt videre til den ekte readeren, og teller radene på veien.</summary>
    private sealed class RowCountingReader(DbDataReader inner, QueryCounter counter) : DbDataReader
    {
        public override bool Read()
        {
            var hasRow = inner.Read();
            if (hasRow) counter.RowRead();
            return hasRow;
        }

        public override async Task<bool> ReadAsync(CancellationToken cancellationToken)
        {
            var hasRow = await inner.ReadAsync(cancellationToken);
            if (hasRow) counter.RowRead();
            return hasRow;
        }

        public override bool NextResult() => inner.NextResult();
        public override Task<bool> NextResultAsync(CancellationToken cancellationToken) => inner.NextResultAsync(cancellationToken);
        public override void Close() => inner.Close();
        public override Task CloseAsync() => inner.CloseAsync();
        public override ValueTask DisposeAsync() => inner.DisposeAsync();

        protected override void Dispose(bool disposing)
        {
            if (disposing) inner.Dispose();
            base.Dispose(disposing);
        }

        public override int Depth => inner.Depth;
        public override int FieldCount => inner.FieldCount;
        public override int VisibleFieldCount => inner.VisibleFieldCount;
        public override bool HasRows => inner.HasRows;
        public override bool IsClosed => inner.IsClosed;
        public override int RecordsAffected => inner.RecordsAffected;
        public override object this[int ordinal] => inner[ordinal];
        public override object this[string name] => inner[name];

        public override bool GetBoolean(int ordinal) => inner.GetBoolean(ordinal);
        public override byte GetByte(int ordinal) => inner.GetByte(ordinal);
        public override long GetBytes(int ordinal, long dataOffset, byte[]? buffer, int bufferOffset, int length) =>
            inner.GetBytes(ordinal, dataOffset, buffer, bufferOffset, length);
        public override char GetChar(int ordinal) => inner.GetChar(ordinal);
        public override long GetChars(int ordinal, long dataOffset, char[]? buffer, int bufferOffset, int length) =>
            inner.GetChars(ordinal, dataOffset, buffer, bufferOffset, length);
        public override string GetDataTypeName(int ordinal) => inner.GetDataTypeName(ordinal);
        public override DateTime GetDateTime(int ordinal) => inner.GetDateTime(ordinal);
        public override decimal GetDecimal(int ordinal) => inner.GetDecimal(ordinal);
        public override double GetDouble(int ordinal) => inner.GetDouble(ordinal);
        public override Type GetFieldType(int ordinal) => inner.GetFieldType(ordinal);
        public override float GetFloat(int ordinal) => inner.GetFloat(ordinal);
        public override Guid GetGuid(int ordinal) => inner.GetGuid(ordinal);
        public override short GetInt16(int ordinal) => inner.GetInt16(ordinal);
        public override int GetInt32(int ordinal) => inner.GetInt32(ordinal);
        public override long GetInt64(int ordinal) => inner.GetInt64(ordinal);
        public override string GetName(int ordinal) => inner.GetName(ordinal);
        public override int GetOrdinal(string name) => inner.GetOrdinal(name);
        public override string GetString(int ordinal) => inner.GetString(ordinal);
        public override object GetValue(int ordinal) => inner.GetValue(ordinal);
        public override int GetValues(object[] values) => inner.GetValues(values);
        public override bool IsDBNull(int ordinal) => inner.IsDBNull(ordinal);
        public override Task<bool> IsDBNullAsync(int ordinal, CancellationToken cancellationToken) =>
            inner.IsDBNullAsync(ordinal, cancellationToken);
        public override T GetFieldValue<T>(int ordinal) => inner.GetFieldValue<T>(ordinal);
        public override Task<T> GetFieldValueAsync<T>(int ordinal, CancellationToken cancellationToken) =>
            inner.GetFieldValueAsync<T>(ordinal, cancellationToken);
        public override Type GetProviderSpecificFieldType(int ordinal) => inner.GetProviderSpecificFieldType(ordinal);
        public override object GetProviderSpecificValue(int ordinal) => inner.GetProviderSpecificValue(ordinal);
        public override int GetProviderSpecificValues(object[] values) => inner.GetProviderSpecificValues(values);
        public override System.Data.DataTable? GetSchemaTable() => inner.GetSchemaTable();
        public override Stream GetStream(int ordinal) => inner.GetStream(ordinal);
        public override TextReader GetTextReader(int ordinal) => inner.GetTextReader(ordinal);
        public override IEnumerator GetEnumerator() => ((IEnumerable)inner).GetEnumerator();
    }
}
