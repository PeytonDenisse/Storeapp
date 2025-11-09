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
    public DbSet<Invoice> Invoice { get; set; } 

    public StoreDbContext(DbContextOptions<StoreDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

     
        modelBuilder.Entity<OrderProduct>()
            .HasKey(p => new { p.OrderId, p.ProductId });

       
        modelBuilder.Entity<Invoice>()
            .HasIndex(i => i.InvoiceNumber)
            .IsUnique();

        modelBuilder.Entity<Order>()
            .HasMany(o => o.Invoices)
            .WithMany(i => i.Orders)
            .UsingEntity<Dictionary<string, object>>(
                "OrderInvoice",
                right => right.HasOne<Invoice>()
                    .WithMany()
                    .HasForeignKey("InvoiceId")
                    .OnDelete(DeleteBehavior.Cascade),
                left => left.HasOne<Order>()
                    .WithMany()
                    .HasForeignKey("OrderId")
                    .OnDelete(DeleteBehavior.Cascade),
                join =>
                {
                    join.HasKey("OrderId", "InvoiceId");
                    join.ToTable("OrderInvoice");
                }
            );
        
        modelBuilder.Entity<Store>().HasData(
            new Store { Id = 1, Description = "Plaza Mayor León", Latitude = 21.1540, Longitude = -101.6946 },
            new Store { Id = 2, Description = "Centro Max", Latitude = 21.0948, Longitude = -101.6417 },
            new Store { Id = 3, Description = "Plaza Galerías Las Torres", Latitude = 21.1211, Longitude = -101.6613 },
            new Store { Id = 4, Description = "Outlet Mulza", Latitude = 21.0459, Longitude = -101.5862 },
            new Store { Id = 5, Description = "La Gran Plaza León", Latitude = 21.1280, Longitude = -101.6827 },
            new Store { Id = 6, Description = "Altacia", Latitude = 21.1280, Longitude = -102 }
        );
        
        modelBuilder.Entity<Product>().HasData(
            new Product { Id = 1, Nombre = "Zapatos de piel", Description = "Calzado típico de León", Price = 1200, StoreId = 4 },
            new Product { Id = 2, Nombre = "Bolsa de cuero", Description = "Artesanía local", Price = 950, StoreId = 4 },
            new Product { Id = 3, Nombre = "Hamburguesa doble", Description = "Comida rápida", Price = 120, StoreId = 1 },
            new Product { Id = 4, Nombre = "Pizza familiar", Description = "Especialidad italiana", Price = 220, StoreId = 2 },
            new Product { Id = 5, Nombre = "Café americano", Description = "Taza grande", Price = 45, StoreId = 3 },
            new Product { Id = 6, Nombre = "Refresco 600ml", Description = "Bebida refrescante", Price = 20, StoreId = 5 }
        );




        // Seed de SystemUser (tu bloque existente)
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