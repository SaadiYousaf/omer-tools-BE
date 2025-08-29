using System;
using System.ComponentModel.DataAnnotations.Schema;
using ProductService.Domain.Entites;

namespace ProductService.Domain.Entities
{
    public class CategoryImage : BaseEntity
    {
        public string CategoryId { get; set; }
        public string ImageUrl { get; set; }
        public string AltText { get; set; }
        public int DisplayOrder { get; set; }
        public bool IsPrimary { get; set; }

        // Navigation property
        [ForeignKey("CategoryId")]
        public Category Category { get; set; }
    }
}