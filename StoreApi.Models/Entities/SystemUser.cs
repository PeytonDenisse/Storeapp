namespace StoreApi.Models.Entities;

public class SystemUser
{
    public int Id { get; set; }
    public string Email { get; set; }
    public string Password { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    
    //Navigation Propierties
    public List<Order> Order { get; set; }
}