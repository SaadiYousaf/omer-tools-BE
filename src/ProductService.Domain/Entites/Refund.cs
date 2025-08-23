namespace ProductService.Domain.Entities
{
    public class Refund : BaseEntity
    {
        public string PaymentId { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; }
        public string Reason { get; set; }
        public string Status { get; set; }

        public Payment Payment { get; set; }
    }
}