using System;
using System.Collections.Generic;
using System.Linq;
using ProductService.Domain.Entities;

namespace ProductService.Domain.Entites
{
    public class Brand : BaseEntity
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string LogoUrl { get; set; }
        public string WebsiteUrl { get; set; }

        // Navigation properties for many-to-many relationship
        public ICollection<BrandCategory> BrandCategories { get; set; } = new List<BrandCategory>();
        public ICollection<Product> Products { get; set; } = new List<Product>();
    }

    public class BrandCategory:BaseEntity
    {
        public string BrandId { get; set; }
        public Brand Brand { get; set; }

        public string CategoryId { get; set; }
        public Category Category { get; set; }
    }
}