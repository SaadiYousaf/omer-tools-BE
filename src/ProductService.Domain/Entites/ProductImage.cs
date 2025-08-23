using ProductService.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ProductService.Domain.Entites
{
    public class ProductImage : BaseEntity
    {
        public string ProductId { get; set; }
        public string ImageUrl { get; set; }
        public string AltText { get; set; }
        public int DisplayOrder { get; set; }
        public bool IsPrimary { get; set; }

        // Navigation property
        public Product Product { get; set; }
    }
}
