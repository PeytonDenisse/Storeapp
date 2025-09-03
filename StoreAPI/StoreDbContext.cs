using Microsoft.EntityFrameworkCore;
using StoreApi.Models.Entities;

namespace StoreAPI;

public class StoreDbContext : DbContext
{
    public DbSet<Order> Order { get; set; }
    public DbSet<Product> Product { get; set; }
    public DbSet<SystemUser> SystemUser { get; set; }
    public DbSet<Store> Store { get; set; }
    public DbSet<OrderProduct> OrderProduct { get; set; }

    public StoreDbContext(DbContextOptions<StoreDbContext> options): base(options)
    {
        
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<OrderProduct>().HasKey(p => new { p.OrderId, p.ProductId });

        modelBuilder.Entity<SystemUser>().HasData(
            new SystemUser
            {
                Id = 1,
                FirstName = "John",
                LastName = "Doe",
                Email = "denisse@gmail.com",
                Password = "12345"
                
            }
        );
    }
}