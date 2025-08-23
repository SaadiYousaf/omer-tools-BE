using ProductService.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ProductService.Domain.Entites
{
    public class ProductVariant : BaseEntity
    {
        public string ProductId { get; set; }
        public string VariantType { get; set; } // "Color", "Size", "Voltage", etc.
        public string VariantValue { get; set; } // "Red", "18V", "Large", etc.
        public decimal PriceAdjustment { get; set; }
        public string SKU { get; set; }
        public int StockQuantity { get; set; }

        // Navigation property
        public Product Product { get; set; }
    }
}
