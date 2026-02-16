using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProductService.Domain.Entites
{
	public class PaginatedResult<T>
	{
		public List<T> Data { get; set; } = new List<T>();
		public int Total { get; set; }
		public int Page { get; set; }
		public int PageSize { get; set; }
		public int TotalPages { get; set; }
		public bool HasPrevious => Page > 1;
		public bool HasNext => Page < TotalPages;
	}
}
