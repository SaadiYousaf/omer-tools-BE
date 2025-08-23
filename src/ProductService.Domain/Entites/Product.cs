using ProductService.Domain.Entites;
using System;
using System.Collections.Generic;

namespace ProductService.Domain.Entities
{
    public class Product : BaseEntity
    {
        public string SubcategoryId { get; set; }
        public string BrandId { get; set; }
        public string SKU { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Specifications { get; set; } // JSON formatted
        public decimal Price { get; set; }
        public bool IsRedemption { get; set; }
        public decimal? DiscountPrice { get; set; }
        public int StockQuantity { get; set; }
        public decimal? Weight { get; set; } // in kg
        public string Dimensions { get; set; } // "LxWxH" format
        public bool IsFeatured { get; set; }
        public string WarrantyPeriod { get; set; } // "1 year", "2 years", etc.

        // Navigation properties
        public Subcategory Subcategory { get; set; }
        public Brand Brand { get; set; }
        public virtual ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();
        public ICollection<ProductVariant> Variants { get; set; } = new List<ProductVariant>();

        // Concurrency control
        [System.ComponentModel.DataAnnotations.Timestamp]
        public byte[] RowVersion { get; set; }
    }
}