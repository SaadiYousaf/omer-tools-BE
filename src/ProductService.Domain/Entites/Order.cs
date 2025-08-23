namespace ProductService.Domain.Entities
{
    public class Order : BaseEntity
    {
        public Order()
        {
            Status = "Pending"; // Default status
            CreatedAt = DateTime.UtcNow;
            IsActive = true;
        }
        public string OrderNumber { get; set; }
        public string UserId { get; set; }
        public string Status { get; set; }
        //public string ShippingAddress { get; set; }
        public string BillingAddress { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal ShippingCost { get; set; }
        public string PaymentStatus { get; set; }
        public string SessionId { get; set; }
        public string TransactionId { get; set; }

        public User User { get; set; }
        public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
        public ICollection<Payment> Payments { get; set; } = new List<Payment>();
        public virtual ShippingAddress ShippingAddress { get; set; }
    }
}