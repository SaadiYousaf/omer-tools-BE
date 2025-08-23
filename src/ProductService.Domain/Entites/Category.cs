using ProductService.Domain.Entites;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProductService.Domain.Entities
{
    public class Category : BaseEntity
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string ImageUrl { get; set; }
        public int DisplayOrder { get; set; }

        // Navigation properties
        public ICollection<BrandCategory> BrandCategories { get; set; } = new List<BrandCategory>();
        public ICollection<Subcategory> Subcategories { get; set; } = new List<Subcategory>();

        // This property can be kept for convenience if needed (but it's not mapped to the database)
        [NotMapped]
        public ICollection<Brand> Brands
        {
            get
            {
                var brands = new List<Brand>();
                foreach (var brandCategory in BrandCategories)
                {
                    brands.Add(brandCategory.Brand);
                }
                return brands;
            }
        }
    }
}