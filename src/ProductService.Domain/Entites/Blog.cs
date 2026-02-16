using ProductService.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProductService.Domain.Entites
{
	public class Blog : BaseEntity
	{
		public string Title { get; set; } = string.Empty;
		public string Slug { get; set; } = string.Empty;
		public string ShortDescription { get; set; } = string.Empty;
		public string Content { get; set; } = string.Empty;
		public string FeaturedImageUrl { get; set; } = string.Empty;
		public string Author { get; set; } = string.Empty;

		// SEO Properties (matching Product pattern)
		public string MetaTitle { get; set; } = string.Empty;
		public string MetaDescription { get; set; } = string.Empty;
		public string MetaKeywords { get; set; } = string.Empty;
		public string CanonicalUrl { get; set; } = string.Empty;
		public string OgTitle { get; set; } = string.Empty;
		public string OgDescription { get; set; } = string.Empty;
		public string OgImage { get; set; } = string.Empty;

		public bool IsPublished { get; set; } = true;
		public bool IsFeatured { get; set; } = false;
		public int ViewCount { get; set; } = 0;
		public DateTime? PublishedAt { get; set; }

		// Navigation properties
		public virtual ICollection<BlogImage> Images { get; set; } = new List<BlogImage>();
	}

	public class BlogImage : BaseEntity
	{
		public string BlogId { get; set; } = string.Empty;
		public string ImageUrl { get; set; } = string.Empty;
		public string AltText { get; set; } = string.Empty;
		public string Caption { get; set; } = string.Empty;
		public int DisplayOrder { get; set; } = 0;
		public bool IsPrimary { get; set; } = false;

		// Navigation property
		public virtual Blog Blog { get; set; } = null!;
	}





	

}
