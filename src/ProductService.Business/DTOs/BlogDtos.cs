using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProductService.Business.DTOs
{
	public class BlogDto
	{
		public string Id { get; set; } = string.Empty;
		public string Title { get; set; } = string.Empty;
		public string Slug { get; set; } = string.Empty;
		public string ShortDescription { get; set; } = string.Empty;
		public string Content { get; set; } = string.Empty;
		public string FeaturedImageUrl { get; set; } = string.Empty;
		public string Author { get; set; } = string.Empty;

		// SEO Properties (matching ProductDto)
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
		public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
		public DateTime? UpdatedAt { get; set; }
		public DateTime? PublishedAt { get; set; }
		public bool IsActive { get; set; } = true;

		public IEnumerable<BlogImageDto> Images { get; set; } = new List<BlogImageDto>();
	}

	// Blog Image DTO (matching ProductImageDto pattern)
	public record BlogImageDto(
		string Id,
		string BlogId,
		string ImageUrl,
		string AltText,
		string Caption,
		int DisplayOrder,
		bool IsPrimary,
		DateTime CreatedAt,
		DateTime? UpdatedAt,
		bool IsActive
	)
	{
		public BlogImageDto() : this("0", "0", "", "", "", 0, false, DateTime.UtcNow, null, true) { }
	}


	public record BlogFullDto(
		BlogDto Blog,
		IEnumerable<BlogImageDto> Images
	)
	{
		public BlogFullDto() : this(new BlogDto(), new List<BlogImageDto>()) { }
	}

	// Paginated Result (if you don't have it already)
	public class BlogPaginatedResult<T>
	{
		public IEnumerable<T> Data { get; set; } = new List<T>();
		public int Total { get; set; }
		public int Page { get; set; }
		public int PageSize { get; set; }
		public int TotalPages { get; set; }
	}

	// Query Parameters
	public class BlogQueryParameters
	{
		public bool? IsFeatured { get; set; }
		public bool? IsPublished { get; set; }
		public string? Search { get; set; }
		public int? Page { get; set; }
		public int? Limit { get; set; }
		public string? SortBy { get; set; } = "CreatedAt";
		public bool? SortDescending { get; set; } = true;
	}

	public class OptimizedBlogQuery : BlogQueryParameters
	{
		public bool IncludeImages { get; set; } = false;
		public bool IncludeCategories { get; set; } = false;
		public bool IncludeTags { get; set; } = false;
	}
}
