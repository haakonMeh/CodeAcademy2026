using Microsoft.EntityFrameworkCore.Design;

namespace Kaffebar.Orm.Data;

/// <summary>
/// Brukes kun av <c>dotnet ef migrations add</c>, som må kunne lage en context uten å kjøre
/// testene. Den kobler seg aldri til databasen når den lager en migrering.
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<KaffebarDbContext>
{
    public KaffebarDbContext CreateDbContext(string[] args) =>
        KaffebarDb.Create("Host=localhost;Database=kaffebar;Username=kaffebar;Password=kaffebar");
}
