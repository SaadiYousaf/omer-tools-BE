namespace ProductService.Domain.Entities
{
    public class OrderItem : BaseEntity
    {
        public string OrderId { get; set; }
        public string ProductId { get; set; }
        public string ProductName { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public string ImageUrl { get; set; }
        public decimal TotalPrice { get; set; }

        public Order Order { get; set; }
    }
}