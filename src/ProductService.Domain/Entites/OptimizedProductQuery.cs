using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProductService.Domain.Entites
{
	public class OptimizedProductQuery
	{
		public int Page { get; set; } = 1;
		public int PageSize { get; set; } = 20;
		public string Search { get; set; }
		public string BrandId { get; set; }
		public string SubcategoryId { get; set; }
		public bool? IsFeatured { get; set; }
		public bool? IsRedemption { get; set; }
		public bool? IsActive { get; set; }
		public string SortBy { get; set; } = "CreatedAt";
		public bool SortDescending { get; set; } = true;
	}

}
