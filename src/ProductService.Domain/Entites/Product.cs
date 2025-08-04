using NpgsqlTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ProductService.Domain.Entites
{
    public class Product : BaseEntity
    {
        public int SubcategoryId { get; set; }
        public int BrandId { get; set; }
        public string SKU { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Specifications { get; set; } // JSON formatted
        public decimal Price { get; set; }
        public decimal? DiscountPrice { get; set; }
        public int StockQuantity { get; set; }
        public decimal? Weight { get; set; } // in kg
        public string Dimensions { get; set; } // "LxWxH" format
        public bool IsFeatured { get; set; }
        public string WarrantyPeriod { get; set; } // "1 year", "2 years", etc.
        public NpgsqlTsVector SearchVector { get; set; }
        // Navigation properties
        public Subcategory Subcategory { get; set; }
        public Brand Brand { get; set; }
        public virtual ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();
        public ICollection<ProductVariant> Variants { get; set; } = new List<ProductVariant>();
    }
}
