using System.ComponentModel.DataAnnotations;

namespace StoreApi.Models.Entities;

public class Invoice
{
    public int Id { get; set; }

    // M:N con Order
    public ICollection<Order> Orders { get; set; } = new List<Order>();

    [Required, MaxLength(64)]
    public string InvoiceNumber { get; set; }

    [Required]
    public DateTime IssueDate { get; set; }

    public DateTime? DueDate { get; set; }

    public double Subtotal { get; set; }
    public double Tax { get; set; }
    public double Total { get; set; }

    [Required, MaxLength(8)]
    public string Currency { get; set; } 

    public bool IsPaid { get; set; }
    public DateTime? PaymentDate { get; set; }

    [Required, MaxLength(200)]
    public string BillingName { get; set; }

    public string BillingAddress { get; set; }

    [EmailAddress]
    public string BillingEmail { get; set; }

    public string TaxId { get; set; } 

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}