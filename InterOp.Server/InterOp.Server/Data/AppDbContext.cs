using InterOp.Server.Domain;
using Microsoft.EntityFrameworkCore;

namespace InterOp.Server.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> opt) : base(opt) { }

    public DbSet<Product> Products => Set<Product>();
    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);

        // Products
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

        // Users
        b.Entity<AppUser>(e =>
        {
            e.ToTable("Users");
            e.HasIndex(u => u.Username).IsUnique();
            e.Property(u => u.Username).HasMaxLength(100);
        });

        // RefreshTokens
        b.Entity<RefreshToken>(e =>
        {
            e.ToTable("RefreshTokens");
            e.HasOne(t => t.User)
             .WithMany(u => u.RefreshTokens)
             .HasForeignKey(t => t.UserId);
            e.Property(t => t.Token).HasMaxLength(512);
            e.HasIndex(t => t.Token).IsUnique();
        });
    }
}
