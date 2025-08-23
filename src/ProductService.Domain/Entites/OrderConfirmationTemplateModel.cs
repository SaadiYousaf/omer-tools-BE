namespace ProductService.Domain.Entities
{
    public class OrderConfirmationTemplateModel
    {
        public Order Order { get; set; }
        public string ContactEmail { get; set; }
        public string SupportPhone { get; set; }
        public string OrderDate { get; set; }
    }
}