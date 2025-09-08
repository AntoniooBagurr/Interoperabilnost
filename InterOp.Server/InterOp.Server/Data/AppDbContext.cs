using InterOp.Server.Domain;
using Microsoft.EntityFrameworkCore;

namespace InterOp.Server.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> opt) : base(opt) { }
    public DbSet<Product> Products => Set<Product>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Product>(e =>
        {
            e.ToTable("Products");
            e.HasIndex(x => x.ExtId).IsUnique();
            e.Property(x => x.Price).HasPrecision(18, 2);     
            e.Property(x => x.Title).HasMaxLength(512);
            e.Property(x => x.Url).HasMaxLength(1024);
            e.Property(x => x.Pic).HasMaxLength(1024);
            e.Property(x => x.Currency).HasMaxLength(8);
        });
    }


}