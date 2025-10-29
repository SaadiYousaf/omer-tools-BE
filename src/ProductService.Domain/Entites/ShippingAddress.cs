using ProductService.Domain.Entities;

public class ShippingAddress
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string FullName { get; set; }
    public string AddressLine1 { get; set; }
    public string AddressLine2 { get; set; }
    public string City { get; set; }
    public string State { get; set; }
    public string PostalCode { get; set; }
    public string Country { get; set; }

    public string PhoneNumber { get; set; } = string.Empty;

    // Foreign key to Order
    public string OrderId { get; set; }
    public Order Order { get; set; }
}