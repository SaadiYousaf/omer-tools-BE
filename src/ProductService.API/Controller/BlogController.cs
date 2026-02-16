using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using ProductService.Business.DTOs;
using ProductService.Business.Interfaces;
using System.Net;

namespace ProductService.API.Controller
{
	[EnableCors("AllowAll")]
	[ApiController]
	[Route("api/[controller]")]
	public class BlogsController : ControllerBase
	{
		private readonly IBlogService _blogService;
		private readonly ILogger<BlogsController> _logger;
		private readonly IConfiguration _configuration;

		public BlogsController(
			IBlogService blogService,
			ILogger<BlogsController> logger,
			IConfiguration configuration)
		{
			_blogService = blogService ?? throw new ArgumentNullException(nameof(blogService));
			_logger = logger ?? throw new ArgumentNullException(nameof(logger));
			_configuration = configuration;
		}

		[HttpGet]
		public async Task<IActionResult> GetAllBlogs([FromQuery] BlogQueryParameters parameters)
		{
			try
			{
				var queryParams = new OptimizedBlogQuery
				{
					IsFeatured = parameters.IsFeatured,
					IsPublished = parameters.IsPublished,
					Search = parameters.Search,
					Page = parameters.Page,
					Limit = parameters.Limit,
					SortBy = parameters.SortBy,
					SortDescending = parameters.SortDescending,
					IncludeImages = true
				};

				var result = await _blogService.GetBlogsOptimizedAsync(queryParams);
				return Ok(result);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error getting blogs");
				return StatusCode(500, "Internal server error");
			}
		}

		[HttpGet("{id}")]
		public async Task<IActionResult> GetBlogById(string id)
		{
			try
			{
				var blog = await _blogService.GetBlogByIdAsync(id);
				return blog == null ? NotFound() : Ok(blog);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Error getting blog with ID {id}");
				return StatusCode(500, "Internal server error");
			}
		}

		[HttpGet("slug/{slug}")]
		public async Task<IActionResult> GetBlogBySlug(string slug)
		{
			try
			{
				var decodedSlug = WebUtility.UrlDecode(slug);
				var blog = await _blogService.GetBlogBySlugAsync(decodedSlug);

				if (blog == null)
					return NotFound();

				// Increment view count
				await _blogService.IncrementViewCountAsync(blog.Blog.Id);

				return Ok(blog);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Error getting blog with slug {slug}");
				return StatusCode(500, "Internal server error");
			}
		}

		[HttpGet("full/{id}")]
		public async Task<IActionResult> GetBlogFullDetails(string id)
		{
			try
			{
				var blog = await _blogService.GetBlogFullDetailsAsync(id);
				if (blog == null)
					return NotFound();

				return Ok(blog);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Error getting full blog details with ID {id}");
				return StatusCode(500, "Internal server error");
			}
		}

		[HttpGet("featured")]
		public async Task<IActionResult> GetFeaturedBlogs([FromQuery] int? count = null)
		{
			try
			{
				var featuredBlogs = await _blogService.GetFeaturedBlogsAsync(count ?? 5);
				return Ok(featuredBlogs);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error getting featured blogs");
				return StatusCode(500, "Internal server error");
			}
		}

		[HttpGet("recent")]
		public async Task<IActionResult> GetRecentBlogs([FromQuery] int? count = null)
		{
			try
			{
				var recentBlogs = await _blogService.GetRecentBlogsAsync(count ?? 5);
				return Ok(recentBlogs);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error getting recent blogs");
				return StatusCode(500, "Internal server error");
			}
		}

		[HttpGet("category/{categoryId}")]
		public async Task<IActionResult> GetBlogsByCategory(
			string categoryId,
			[FromQuery] int page = 1,
			[FromQuery] int pageSize = 10)
		{
			try
			{
				var result = await _blogService.GetBlogsByCategoryAsync(categoryId, page, pageSize);
				return Ok(result);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Error getting blogs for category {categoryId}");
				return StatusCode(500, "Internal server error");
			}
		}

		[HttpGet("tag/{tagId}")]
		public async Task<IActionResult> GetBlogsByTag(
			string tagId,
			[FromQuery] int page = 1,
			[FromQuery] int pageSize = 10)
		{
			try
			{
				var result = await _blogService.GetBlogsByTagAsync(tagId, page, pageSize);
				return Ok(result);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Error getting blogs for tag {tagId}");
				return StatusCode(500, "Internal server error");
			}
		}

		[HttpPost]
		public async Task<IActionResult> CreateBlog([FromBody] BlogDto blogDto)
		{
			try
			{
				if (!ModelState.IsValid)
					return BadRequest(ModelState);

				var createdBlog = await _blogService.CreateBlogAsync(blogDto);
				return CreatedAtAction(
					nameof(GetBlogById),
					new { id = createdBlog.Id },
					createdBlog
				);
			}
			catch (ArgumentException ex)
			{
				_logger.LogWarning(ex, "Invalid argument when creating blog");
				return BadRequest(ex.Message);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error creating blog");
				return StatusCode(500, "Internal server error");
			}
		}

		[HttpPut("{id}")]
		public async Task<IActionResult> UpdateBlog(string id, [FromBody] BlogDto blogDto)
		{
			try
			{
				if (!ModelState.IsValid)
					return BadRequest(ModelState);

				if (id != blogDto.Id)
					return BadRequest("ID mismatch");

				await _blogService.UpdateBlogAsync(blogDto);
				return Ok(new { success = true, message = "Blog updated successfully" });
			}
			catch (KeyNotFoundException ex)
			{
				_logger.LogWarning(ex, $"Blog with ID {id} not found");
				return NotFound();
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Error updating blog with ID {id}");
				return StatusCode(500, "Internal server error");
			}
		}

		[HttpDelete("{id}")]
		public async Task<IActionResult> DeleteBlog(string id)
		{
			try
			{
				await _blogService.DeleteBlogAsync(id);
				return NoContent();
			}
			catch (KeyNotFoundException ex)
			{
				_logger.LogWarning(ex, $"Blog with ID {id} not found");
				return NotFound();
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Error deleting blog with ID {id}");
				return StatusCode(500, "Internal server error");
			}
		}

		//[HttpGet("categories")]
		//public async Task<IActionResult> GetAllCategories()
		//{
		//	try
		//	{
		//		var categories = await _blogService.GetAllBlogCategoriesAsync();
		//		return Ok(categories);
		//	}
		//	catch (Exception ex)
		//	{
		//		_logger.LogError(ex, "Error getting blog categories");
		//		return StatusCode(500, "Internal server error");
		//	}
		//}

		//[HttpGet("tags")]
		//public async Task<IActionResult> GetAllTags()
		//{
		//	try
		//	{
		//		var tags = await _blogService.GetAllBlogTagsAsync();
		//		return Ok(tags);
		//	}
		//	catch (Exception ex)
		//	{
		//		_logger.LogError(ex, "Error getting blog tags");
		//		return StatusCode(500, "Internal server error");
		//	}
		//}

		[HttpPost("images")]
		public async Task<IActionResult> UploadBlogImage(
			[FromForm] IFormFile file,
			[FromForm] string blogId,
			[FromForm] string altText = "",
			[FromForm] string caption = "",
			[FromForm] int displayOrder = 0,
			[FromForm] bool isPrimary = false)
		{
			try
			{
				if (file == null || file.Length == 0)
					return BadRequest(new { success = false, message = "No file uploaded" });

				if (string.IsNullOrEmpty(blogId))
					return BadRequest(new { success = false, message = "Invalid blog ID" });

				// Validate file type
				var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
				var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
				if (string.IsNullOrEmpty(extension) || !allowedExtensions.Contains(extension))
					return BadRequest(new
					{
						success = false,
						message = $"Invalid file type. Allowed: {string.Join(", ", allowedExtensions)}"
					});

				// Ensure directories exist
				var webRootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
				Directory.CreateDirectory(webRootPath);

				var uploadsDir = Path.Combine(webRootPath, "uploads", "blogs");
				Directory.CreateDirectory(uploadsDir);

				// Generate unique filename
				var fileName = $"{Guid.NewGuid()}{extension}";
				var filePath = Path.Combine(uploadsDir, fileName);

				// Save file
				using (var stream = new FileStream(filePath, FileMode.Create))
				{
					await file.CopyToAsync(stream);
				}

				// Create image record
				var imageDto = new BlogImageDto(
					Id: Guid.NewGuid().ToString(),
					BlogId: blogId,
					ImageUrl: $"/uploads/blogs/{fileName}",
					AltText: altText,
					Caption: caption,
					DisplayOrder: displayOrder,
					IsPrimary: isPrimary,
					CreatedAt: DateTime.UtcNow,
					UpdatedAt: null,
					IsActive: true
				);

				var createdImage = await _blogService.CreateBlogImageAsync(imageDto);

				return Ok(new { success = true, image = createdImage });
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Blog image upload failed");
				return StatusCode(500, new
				{
					success = false,
					message = ex.Message
				});
			}
		}

		[HttpDelete("images/{imageId}")]
		public async Task<IActionResult> DeleteBlogImage(string imageId)
		{
			try
			{
				if (string.IsNullOrEmpty(imageId))
					return BadRequest(new { success = false, message = "Invalid image ID" });

				// Get the image first to check if it exists and get file path
				var image = await _blogService.GetBlogImageByIdAsync(imageId);
				if (image == null)
					return NotFound(new { success = false, message = "Image not found" });

				// Delete the physical file
				if (!string.IsNullOrEmpty(image.ImageUrl))
				{
					var webRootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
					var filePath = Path.Combine(webRootPath, image.ImageUrl.TrimStart('/'));

					if (System.IO.File.Exists(filePath))
					{
						System.IO.File.Delete(filePath);
						_logger.LogInformation($"Deleted physical file: {filePath}");
					}
				}

				// Delete the image record from database
				await _blogService.DeleteBlogImageAsync(imageId);

				return Ok(new
				{
					success = true,
					message = "Image deleted successfully",
					deletedImageId = imageId
				});
			}
			catch (KeyNotFoundException ex)
			{
				_logger.LogWarning(ex, $"Image with ID {imageId} not found");
				return NotFound(new { success = false, message = "Image not found" });
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Error deleting image with ID {imageId}");
				return StatusCode(500, new
				{
					success = false,
					message = "Internal server error while deleting image"
				});
			}
		}

		[HttpPut("{id}/view")]
		public async Task<IActionResult> IncrementViewCount(string id)
		{
			try
			{
				await _blogService.IncrementViewCountAsync(id);
				return Ok(new { success = true, message = "View count incremented" });
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Error incrementing view count for blog {id}");
				return StatusCode(500, "Internal server error");
			}
		}

		[HttpGet("seo/{id}")]
		public async Task<IActionResult> GetBlogSEODetails(string id)
		{
			try
			{
				var blog = await _blogService.GetBlogByIdAsync(id);
				if (blog == null)
					return NotFound();

				var baseUrl = _configuration["AppSettings:BaseUrl"] ?? $"{Request.Scheme}://{Request.Host}";
				var canonicalUrl = !string.IsNullOrEmpty(blog.CanonicalUrl)
					? $"{baseUrl.TrimEnd('/')}{blog.CanonicalUrl}"
					: $"{baseUrl.TrimEnd('/')}/blog/{blog.Slug}";

				return Ok(new
				{
					metaTitle = !string.IsNullOrEmpty(blog.MetaTitle) ? blog.MetaTitle : blog.Title,
					metaDescription = !string.IsNullOrEmpty(blog.MetaDescription) ? blog.MetaDescription : blog.ShortDescription,
					metaKeywords = blog.MetaKeywords,
					canonicalUrl = canonicalUrl,
					ogTitle = !string.IsNullOrEmpty(blog.OgTitle) ? blog.OgTitle : blog.Title,
					ogDescription = !string.IsNullOrEmpty(blog.OgDescription) ? blog.OgDescription : blog.ShortDescription,
					ogImage = !string.IsNullOrEmpty(blog.OgImage) ? blog.OgImage : blog.FeaturedImageUrl,
					blogTitle = blog.Title,
					blogDescription = blog.ShortDescription,
					author = blog.Author,
					publishedAt = blog.PublishedAt ?? blog.CreatedAt,
					modifiedAt = blog.UpdatedAt ?? blog.CreatedAt
				});
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Error getting SEO details for blog with ID {id}");
				return StatusCode(500, "Internal server error");
			}
		}

		[HttpGet("sitemap")]
		public async Task<IActionResult> GetBlogsSitemapData()
		{
			try
			{
				var blogs = await _blogService.GetAllBlogsAsync();
				var baseUrl = _configuration["AppSettings:BaseUrl"] ?? $"{Request.Scheme}://{Request.Host}";

				var sitemapEntries = blogs.Where(b => b.IsPublished && b.IsActive).Select(b => new
				{
					url = $"{baseUrl.TrimEnd('/')}/blog/{b.Slug}",
					lastModified = b.UpdatedAt ?? b.CreatedAt,
					changeFrequency = "weekly",
					priority = b.IsFeatured ? 0.8 : 0.6
				}).ToList();

				return Ok(sitemapEntries);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error generating sitemap data for blogs");
				return StatusCode(500, "Internal server error");
			}
		}

		[HttpGet("search")]
		public async Task<IActionResult> SearchBlogs(
			[FromQuery] string query,
			[FromQuery] int page = 1,
			[FromQuery] int pageSize = 10)
		{
			try
			{
				if (string.IsNullOrWhiteSpace(query))
					return BadRequest(new { success = false, message = "Search query is required" });

				var queryParams = new OptimizedBlogQuery
				{
					Search = query,
					Page = page,
					Limit = pageSize,
					IsPublished = true,
					IncludeImages = true,
					SortBy = "CreatedAt",
					SortDescending = true
				};

				var result = await _blogService.GetBlogsOptimizedAsync(queryParams);
				return Ok(result);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Error searching blogs with query: {query}");
				return StatusCode(500, "Internal server error");
			}
		}

		[HttpGet("archive/{year}/{month?}")]
		public async Task<IActionResult> GetBlogsByArchive(
			int year,
			int? month = null,
			[FromQuery] int page = 1,
			[FromQuery] int pageSize = 10)
		{
			try
			{
				var allBlogs = await _blogService.GetAllBlogsAsync();
				var publishedBlogs = allBlogs.Where(b => b.IsPublished && b.IsActive);

				var filteredBlogs = publishedBlogs.Where(b =>
				{
					var date = b.PublishedAt ?? b.CreatedAt;
					if (month.HasValue)
						return date.Year == year && date.Month == month.Value;
					return date.Year == year;
				});

				var total = filteredBlogs.Count();
				var totalPages = (int)Math.Ceiling(total / (double)pageSize);
				var skip = (page - 1) * pageSize;

				var paginatedBlogs = filteredBlogs
					.OrderByDescending(b => b.PublishedAt ?? b.CreatedAt)
					.Skip(skip)
					.Take(pageSize)
					.ToList();

				return Ok(new
				{
					data = paginatedBlogs,
					total = total,
					page = page,
					pageSize = pageSize,
					totalPages = totalPages,
					year = year,
					month = month
				});
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Error getting blogs for archive {year}/{month}");
				return StatusCode(500, "Internal server error");
			}
		}

		[HttpGet("author/{authorName}")]
		public async Task<IActionResult> GetBlogsByAuthor(
			string authorName,
			[FromQuery] int page = 1,
			[FromQuery] int pageSize = 10)
		{
			try
			{
				var queryParams = new OptimizedBlogQuery
				{
					Search = authorName,
					Page = page,
					Limit = pageSize,
					IsPublished = true,
					IncludeImages = true,
					SortBy = "CreatedAt",
					SortDescending = true
				};

				var result = await _blogService.GetBlogsOptimizedAsync(queryParams);

				// Filter by author
				var filteredData = result.Data.Where(b =>
					b.Author.Equals(authorName, StringComparison.OrdinalIgnoreCase)).ToList();

				return Ok(new
				{
					data = filteredData,
					total = filteredData.Count,
					page = page,
					pageSize = pageSize,
					totalPages = (int)Math.Ceiling(filteredData.Count / (double)pageSize),
					author = authorName
				});
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Error getting blogs by author: {authorName}");
				return StatusCode(500, "Internal server error");
			}
		}

		[HttpGet("popular")]
		public async Task<IActionResult> GetPopularBlogs([FromQuery] int? count = null)
		{
			try
			{
				var blogs = await _blogService.GetAllBlogsAsync();
				var popularBlogs = blogs
					.Where(b => b.IsPublished && b.IsActive)
					.OrderByDescending(b => b.ViewCount)
					.ThenByDescending(b => b.CreatedAt)
					.Take(count ?? 5)
					.ToList();

				return Ok(popularBlogs);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error getting popular blogs");
				return StatusCode(500, "Internal server error");
			}
		}

		//[HttpGet("related/{blogId}")]
		//public async Task<IActionResult> GetRelatedBlogs(string blogId, [FromQuery] int count = 3)
		//{
		//	try
		//	{
		//		var blog = await _blogService.GetBlogFullDetailsAsync(blogId);
		//		if (blog == null)
		//			return NotFound();

		//		var allBlogs = await _blogService.GetAllBlogsAsync();
		//		var publishedBlogs = allBlogs.Where(b => b.IsPublished && b.IsActive && b.Id != blogId);

		//		// Find blogs with same categories
		//		var categoryIds = blog.Categories.Select(c => c.Id);
		//		var relatedByCategory = publishedBlogs
		//			.Where(b => b.Categories.Any(c => categoryIds.Contains(c.Id)))
		//			.OrderByDescending(b => b.ViewCount)
		//			.Take(count)
		//			.ToList();

		//		// If not enough by category, add recent blogs
		//		if (relatedByCategory.Count < count)
		//		{
		//			var remaining = count - relatedByCategory.Count;
		//			var recentBlogs = publishedBlogs
		//				.Where(b => !relatedByCategory.Any(r => r.Id == b.Id))
		//				.OrderByDescending(b => b.CreatedAt)
		//				.Take(remaining)
		//				.ToList();

		//			relatedByCategory.AddRange(recentBlogs);
		//		}

		//		return Ok(relatedByCategory.Take(count));
		//	}
		//	catch (Exception ex)
		//	{
		//		_logger.LogError(ex, $"Error getting related blogs for {blogId}");
		//		return StatusCode(500, "Internal server error");
		//	}
		//}

		[HttpPost("bulk-status")]
		public async Task<IActionResult> UpdateBlogsBulkStatus([FromBody] BulkStatusUpdateDto updateDto)
		{
			try
			{
				if (updateDto == null || updateDto.BlogIds == null || !updateDto.BlogIds.Any())
					return BadRequest(new { success = false, message = "Invalid request data" });

				var updatedCount = 0;
				foreach (var blogId in updateDto.BlogIds)
				{
					try
					{
						var blog = await _blogService.GetBlogByIdAsync(blogId);
						if (blog != null)
						{
							if (updateDto.IsPublished.HasValue)
								blog.IsPublished = updateDto.IsPublished.Value;

							if (updateDto.IsFeatured.HasValue)
								blog.IsFeatured = updateDto.IsFeatured.Value;

							if (updateDto.IsActive.HasValue)
								blog.IsActive = updateDto.IsActive.Value;

							await _blogService.UpdateBlogAsync(blog);
							updatedCount++;
						}
					}
					catch (Exception ex)
					{
						_logger.LogWarning(ex, $"Error updating blog {blogId}");
					}
				}

				return Ok(new
				{
					success = true,
					message = $"Successfully updated {updatedCount} out of {updateDto.BlogIds.Count()} blogs",
					updatedCount = updatedCount
				});
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error in bulk status update");
				return StatusCode(500, "Internal server error");
			}
		}

		[HttpPost("import")]
		public async Task<IActionResult> ImportBlogs([FromBody] List<BlogDto> blogDtos)
		{
			try
			{
				if (blogDtos == null || !blogDtos.Any())
					return BadRequest(new { success = false, message = "No blogs to import" });

				var importedCount = 0;
				var errors = new List<string>();

				foreach (var blogDto in blogDtos)
				{
					try
					{
						await _blogService.CreateBlogAsync(blogDto);
						importedCount++;
					}
					catch (Exception ex)
					{
						errors.Add($"Error importing blog '{blogDto.Title}': {ex.Message}");
						_logger.LogWarning(ex, $"Error importing blog: {blogDto.Title}");
					}
				}

				return Ok(new
				{
					success = true,
					message = $"Successfully imported {importedCount} out of {blogDtos.Count} blogs",
					importedCount = importedCount,
					errorCount = errors.Count,
					errors = errors
				});
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error importing blogs");
				return StatusCode(500, "Internal server error");
			}
		}
	}

	// DTOs for additional operations
	public class BulkStatusUpdateDto
	{
		public List<string> BlogIds { get; set; } = new List<string>();
		public bool? IsPublished { get; set; }
		public bool? IsFeatured { get; set; }
		public bool? IsActive { get; set; }
	}

	public class ArchiveResponse
	{
		public List<BlogDto> Data { get; set; } = new List<BlogDto>();
		public int Total { get; set; }
		public int Page { get; set; }
		public int PageSize { get; set; }
		public int TotalPages { get; set; }
		public int Year { get; set; }
		public int? Month { get; set; }
	}
}
