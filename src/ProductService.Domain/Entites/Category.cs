using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ProductService.Domain.Entites
{
    public class Category : BaseEntity
    {
        public int BrandId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string ImageUrl { get; set; }
        public int DisplayOrder { get; set; }

        public Brand Brand { get; set; }
        public ICollection<Subcategory> Subcategories { get; set; } = new List<Subcategory>();
    }
}
