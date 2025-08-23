namespace ProductService.Domain.Entities
{
    public class Payment : BaseEntity
    {
        public string OrderId { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; }
        public string PaymentMethod { get; set; }
        public string Status { get; set; }
        public string TransactionId { get; set; }
        public string Last4Digits { get; set; }
        public string CardBrand { get; set; }
        public int? ExpiryMonth { get; set; }
        public int? ExpiryYear { get; set; }
        public string CustomerEmail { get; set; }

        public Order Order { get; set; }
        public ICollection<Refund> Refunds { get; set; } = new List<Refund>();
    }
}