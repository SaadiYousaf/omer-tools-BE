namespace ProductService.Domain.Entities
{
    public class Subcategory : BaseEntity
    {
        public string CategoryId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string ImageUrl { get; set; }
        public int DisplayOrder { get; set; }

        public Category Category { get; set; }
        public ICollection<Product> Products { get; set; } = new List<Product>();
    }
}