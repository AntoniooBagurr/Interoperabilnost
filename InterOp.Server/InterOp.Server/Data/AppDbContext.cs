using InterOp.Server.Domain;
using Microsoft.EntityFrameworkCore;

namespace InterOp.Server.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> opt) : base(opt) { }
    public DbSet<Product> Products => Set<Product>();
}