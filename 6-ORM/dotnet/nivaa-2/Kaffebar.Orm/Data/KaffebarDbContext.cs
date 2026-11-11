using Kaffebar.Orm.Domain;
using Microsoft.EntityFrameworkCore;

namespace Kaffebar.Orm.Data;

public class KaffebarDbContext(DbContextOptions<KaffebarDbContext> options) : DbContext(options)
{
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderLine> OrderLines => Set<OrderLine>();
    public DbSet<Bean> Beans => Set<Bean>();
    public DbSet<Roastery> Roasteries => Set<Roastery>();
    public DbSet<StatusChange> StatusChanges => Set<StatusChange>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Roastery>(entity =>
        {
            entity.Property(r => r.Name).HasMaxLength(100);
            entity.Property(r => r.City).HasMaxLength(100);
            entity.HasIndex(r => r.Name).IsUnique();
        });

        modelBuilder.Entity<Bean>(entity =>
        {
            entity.Property(b => b.Name).HasMaxLength(100);
            entity.Property(b => b.Origin).HasMaxLength(100);
            entity.Property(b => b.Process).HasMaxLength(50);
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.Property(o => o.CustomerName).HasMaxLength(50);
            entity.Property(o => o.TotalPrice).HasPrecision(10, 2);
            // Enums som tekst, ikke tall: «PickedUp» er lesbart i DBeaver og i rå SQL, 3 er det ikke.
            entity.Property(o => o.Status).HasConversion<string>().HasMaxLength(20);
            entity.HasIndex(o => o.CreatedAt);

            entity.HasMany(o => o.Lines).WithOne(l => l.Order).HasForeignKey(l => l.OrderId);
            entity.HasMany(o => o.StatusHistory).WithOne().HasForeignKey(s => s.OrderId);
        });

        modelBuilder.Entity<OrderLine>(entity =>
        {
            entity.Property(l => l.DrinkName).HasMaxLength(50);
            entity.Property(l => l.UnitPrice).HasPrecision(10, 2);
            entity.Property(l => l.Size).HasConversion<string>().HasMaxLength(20);
            entity.Property(l => l.MilkType).HasConversion<string>().HasMaxLength(20);
        });

        modelBuilder.Entity<StatusChange>(entity =>
        {
            entity.Property(s => s.Status).HasConversion<string>().HasMaxLength(20);
        });
    }
}
