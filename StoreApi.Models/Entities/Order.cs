namespace StoreApi.Models.Entities;

public class Order
{
    public int Id { get; set; }
    public int SystemUserId { get; set; }
    public SystemUser SystemUser { get; set; }
    
    public double Total { get; set; }
    public DateTime CreatedAt { get; set; }
    
    public List<OrderProduct> OrderProducts { get; set; }
    public List<Invoice> Invoices { get; set; } = new();
}