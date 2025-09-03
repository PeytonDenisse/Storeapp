namespace StoreApi.Models.Entities;

public class Product
{
    public int Id { get; set; }
    
    public string Nombre { get; set; }
    public string Description { get; set; }
    public double Price { get; set; }
    public Store Store { get; set; }
    
    
    public int StoreId { get; set; }
    
}