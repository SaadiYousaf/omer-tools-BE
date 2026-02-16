using AutoMapper;
using ProductService.Business.DTOs;
using ProductService.Business.Interfaces;
using ProductService.Domain.Entites;
using ProductService.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ProductService.Business.Services
{
	public class BlogService : IBlogService
	{
		private readonly IUnitOfWork _unitOfWork;
		private readonly IMapper _mapper;

		public BlogService(IUnitOfWork unitOfWork, IMapper mapper)
		{
			_unitOfWork = unitOfWork;
			_mapper = mapper;
		}

		#region Blog Methods

		public async Task<BlogDto> GetBlogByIdAsync(string id)
		{
			var blog = await _unitOfWork.BlogRepository.GetByIdAsync(id, "Images");
			return _mapper.Map<BlogDto>(blog);
		}

		public async Task<BlogFullDto> GetBlogFullDetailsAsync(string id)
		{
			var blog = await _unitOfWork.BlogRepository.GetByIdAsync(
				id,
				"Images"
			);

			if (blog == null) return null;

			return new BlogFullDto(
				_mapper.Map<BlogDto>(blog),
				_mapper.Map<IEnumerable<BlogImageDto>>(blog.Images)
			);
		}

		public async Task<BlogFullDto> GetBlogBySlugAsync(string slug)
		{
			// Use GetAsync with predicate instead of GetBySlugAsync
			var blogs = await _unitOfWork.BlogRepository.GetAsync(
				b => b.Slug == slug && b.IsActive,
				"Images"
			);

			var blog = blogs.FirstOrDefault();
			if (blog == null) return null;

			return new BlogFullDto(
				_mapper.Map<BlogDto>(blog),
				_mapper.Map<IEnumerable<BlogImageDto>>(blog.Images)
			);
		}

		public async Task<IEnumerable<BlogDto>> GetAllBlogsAsync()
		{
			var blogs = await _unitOfWork.BlogRepository.GetAllAsync("Images");
			return _mapper.Map<IEnumerable<BlogDto>>(blogs);
		}

		public async Task<PaginatedResult<BlogDto>> GetBlogsOptimizedAsync(OptimizedBlogQuery queryParams)
		{
			var query = _unitOfWork.BlogRepository.GetQueryable()
				.Where(b => b.IsActive);

			// Apply filters
			if (queryParams.IsPublished.HasValue)
				query = query.Where(b => b.IsPublished == queryParams.IsPublished.Value);

			if (queryParams.IsFeatured.HasValue)
				query = query.Where(b => b.IsFeatured == queryParams.IsFeatured.Value);


			if (!string.IsNullOrEmpty(queryParams.Search))
			{
				query = query.Where(b =>
					b.Title.Contains(queryParams.Search) ||
					b.ShortDescription.Contains(queryParams.Search) ||
					b.Content.Contains(queryParams.Search)||
					b.Author.Contains(queryParams.Search));
			}

			// Apply sorting
			query = queryParams.SortDescending == true
				? query.OrderByDescending(GetSortProperty(queryParams.SortBy))
				: query.OrderBy(GetSortProperty(queryParams.SortBy));

			// Get total count
			var total = await query.CountAsync();

			// Apply pagination
			var page = queryParams.Page ?? 1;
			var limit = queryParams.Limit ?? 10;
			var skip = (page - 1) * limit;

			var data = await query
				.Skip(skip)
				.Take(limit)
				.Include("Images")
				.ToListAsync();

			// Map to DTO
			var blogDtos = _mapper.Map<IEnumerable<BlogDto>>(data);

			return new PaginatedResult<BlogDto>
			{
				Data = blogDtos.ToList() ,
				Total = total,
				Page = page,
				PageSize = limit,
				TotalPages = (int)Math.Ceiling(total / (double)limit)
			};
		}

		private System.Linq.Expressions.Expression<Func<Blog, object>> GetSortProperty(string sortBy)
		{
			return sortBy?.ToLower() switch
			{
				"title" => b => b.Title,
				"viewcount" => b => b.ViewCount,
				"publishedat" => b => b.PublishedAt ?? b.CreatedAt,
				_ => b => b.CreatedAt
			};
		}

		public async Task<PaginatedResult<BlogDto>> GetBlogsByCategoryAsync(string categoryId, int page = 1, int pageSize = 10)
		{
			var queryParams = new OptimizedBlogQuery
			{
				Page = page,
				Limit = pageSize,
				IsPublished = true,
				IncludeImages = true
			};

			return await GetBlogsOptimizedAsync(queryParams);
		}

		public async Task<PaginatedResult<BlogDto>> GetBlogsByTagAsync(string tagId, int page = 1, int pageSize = 10)
		{
			var queryParams = new OptimizedBlogQuery
			{
				Page = page,
				Limit = pageSize,
				IsPublished = true,
				IncludeImages = true
			};

			return await GetBlogsOptimizedAsync(queryParams);
		}

		public async Task<IEnumerable<BlogDto>> GetFeaturedBlogsAsync(int count = 5)
		{
			var blogs = await _unitOfWork.BlogRepository.GetAllAsync("Images");
			return _mapper.Map<IEnumerable<BlogDto>>(
				blogs.Where(b => b.IsFeatured && b.IsPublished && b.IsActive)
					 .OrderByDescending(b => b.CreatedAt)
					 .Take(count)
			);
		}

		public async Task<IEnumerable<BlogDto>> GetRecentBlogsAsync(int count = 5)
		{
			var blogs = await _unitOfWork.BlogRepository.GetAllAsync("Images");
			return _mapper.Map<IEnumerable<BlogDto>>(
				blogs.Where(b => b.IsPublished && b.IsActive)
					 .OrderByDescending(b => b.CreatedAt)
					 .Take(count)
			);
		}

		public async Task<BlogDto> CreateBlogAsync(BlogDto blogDto)
		{
			// Generate ID if not provided
			if (string.IsNullOrEmpty(blogDto.Id) || blogDto.Id == "0")
			{
				blogDto.Id = Guid.NewGuid().ToString();
			}

			// Generate slug if not provided
			if (string.IsNullOrEmpty(blogDto.Slug))
			{
				blogDto.Slug = GenerateSlug(blogDto.Title);
			}

			// Set published date if publishing
			if (blogDto.IsPublished && !blogDto.PublishedAt.HasValue)
			{
				blogDto.PublishedAt = DateTime.UtcNow;
			}

			// Map DTO to entity
			var blog = _mapper.Map<Blog>(blogDto);

			// Set canonical URL
			blog.CanonicalUrl = GenerateCanonicalUrl(blog.Slug);

			await _unitOfWork.BlogRepository.AddAsync(blog);
			await _unitOfWork.CompleteAsync();

			return _mapper.Map<BlogDto>(blog);
		}

		public async Task UpdateBlogAsync(BlogDto blogDto)
		{
			var existingBlog = await _unitOfWork.BlogRepository.GetByIdAsync(blogDto.Id);
			if (existingBlog == null)
				throw new KeyNotFoundException($"Blog with ID {blogDto.Id} not found");

			// Update slug if title changed
			if (existingBlog.Title != blogDto.Title && string.IsNullOrEmpty(blogDto.Slug))
			{
				blogDto.Slug = GenerateSlug(blogDto.Title);
			}

			// Update canonical URL
			blogDto.CanonicalUrl = GenerateCanonicalUrl(blogDto.Slug);

			// Update basic properties
			_mapper.Map(blogDto, existingBlog);
			existingBlog.UpdatedAt = DateTime.UtcNow;

		

			await _unitOfWork.BlogRepository.UpdateAsync(existingBlog);
			await _unitOfWork.CompleteAsync();
		}

		public async Task DeleteBlogAsync(string id)
		{
			var blog = await _unitOfWork.BlogRepository.GetByIdAsync(id);
			if (blog == null)
				throw new KeyNotFoundException($"Blog with ID {id} not found");

			await _unitOfWork.BlogRepository.DeleteAsync(blog);
			await _unitOfWork.CompleteAsync();
		}

		public async Task IncrementViewCountAsync(string id)
		{
			var blog = await _unitOfWork.BlogRepository.GetByIdAsync(id);
			if (blog == null) return;

			blog.ViewCount++;
			blog.UpdatedAt = DateTime.UtcNow;

			await _unitOfWork.BlogRepository.UpdateAsync(blog);
			await _unitOfWork.CompleteAsync();
		}

		private string GenerateSlug(string title)
		{
			if (string.IsNullOrWhiteSpace(title))
				return string.Empty;

			var slug = title.ToLowerInvariant();

			// Replace spaces with hyphens
			slug = slug.Replace(' ', '-');

			// Remove invalid characters
			slug = System.Text.RegularExpressions.Regex.Replace(slug, @"[^a-z0-9\s-]", "");

			// Replace multiple hyphens with single hyphen
			slug = System.Text.RegularExpressions.Regex.Replace(slug, @"\s+", " ").Trim();
			slug = System.Text.RegularExpressions.Regex.Replace(slug, @"\s", "-");
			slug = System.Text.RegularExpressions.Regex.Replace(slug, @"-+", "-");

			return slug;
		}

		private string GenerateCanonicalUrl(string slug)
		{
			return $"/blog/{slug}";
		}

		#endregion

		#region BlogCategory Methods

		//public async Task<BlogCategoryDto> GetBlogCategoryByIdAsync(string id)
		//{
		//	var category = await _unitOfWork.BlogCategoryRepository.GetByIdAsync(id);
		//	return _mapper.Map<BlogCategoryDto>(category);
		//}

		//public async Task<IEnumerable<BlogCategoryDto>> GetAllBlogCategoriesAsync()
		//{
		//	var categories = await _unitOfWork.BlogCategoryRepository.GetAllAsync();
		//	return _mapper.Map<IEnumerable<BlogCategoryDto>>(categories);
		//}

		//public async Task<BlogCategoryDto> CreateBlogCategoryAsync(BlogCategoryDto categoryDto)
		//{

		//	var category = _mapper.Map<BlogCategory>(categoryDto);
		//	await _unitOfWork.BlogCategoryRepository.AddAsync(category);
		//	await _unitOfWork.CompleteAsync();

		//	return _mapper.Map<BlogCategoryDto>(category);
		//}

		//public async Task UpdateBlogCategoryAsync(BlogCategoryDto categoryDto)
		//{
		//	var existingCategory = await _unitOfWork.BlogCategoryRepository.GetByIdAsync(categoryDto.Id);
		//	if (existingCategory == null)
		//		throw new KeyNotFoundException($"Blog category with ID {categoryDto.Id} not found");

		//	_mapper.Map(categoryDto, existingCategory);
		//	existingCategory.UpdatedAt = DateTime.UtcNow;

		//	await _unitOfWork.BlogCategoryRepository.UpdateAsync(existingCategory);
		//	await _unitOfWork.CompleteAsync();
		//}

		//public async Task DeleteBlogCategoryAsync(string id)
		//{
		//	var category = await _unitOfWork.BlogCategoryRepository.GetByIdAsync(id);
		//	if (category == null)
		//		throw new KeyNotFoundException($"Blog category with ID {id} not found");

		//	await _unitOfWork.BlogCategoryRepository.DeleteAsync(category);
		//	await _unitOfWork.CompleteAsync();
		//}

		#endregion

		#region BlogTag Methods

		//public async Task<BlogTagDto> GetBlogTagByIdAsync(string id)
		//{
		//	var tag = await _unitOfWork.BlogTagRepository.GetByIdAsync(id);
		//	return _mapper.Map<BlogTagDto>(tag);
		//}

		//public async Task<IEnumerable<BlogTagDto>> GetAllBlogTagsAsync()
		//{
		//	var tags = await _unitOfWork.BlogTagRepository.GetAllAsync();
		//	return _mapper.Map<IEnumerable<BlogTagDto>>(tags);
		//}

		//public async Task<BlogTagDto> CreateBlogTagAsync(BlogTagDto tagDto)
		//{

		//	var tag = _mapper.Map<BlogTag>(tagDto);
		//	await _unitOfWork.BlogTagRepository.AddAsync(tag);
		//	await _unitOfWork.CompleteAsync();

		//	return _mapper.Map<BlogTagDto>(tag);
		//}

		//public async Task UpdateBlogTagAsync(BlogTagDto tagDto)
		//{
		//	var existingTag = await _unitOfWork.BlogTagRepository.GetByIdAsync(tagDto.Id);
		//	if (existingTag == null)
		//		throw new KeyNotFoundException($"Blog tag with ID {tagDto.Id} not found");

		//	_mapper.Map(tagDto, existingTag);
		//	existingTag.UpdatedAt = DateTime.UtcNow;

		//	await _unitOfWork.BlogTagRepository.UpdateAsync(existingTag);
		//	await _unitOfWork.CompleteAsync();
		//}

		//public async Task DeleteBlogTagAsync(string id)
		//{
		//	var tag = await _unitOfWork.BlogTagRepository.GetByIdAsync(id);
		//	if (tag == null)
		//		throw new KeyNotFoundException($"Blog tag with ID {id} not found");

		//	await _unitOfWork.BlogTagRepository.DeleteAsync(tag);
		//	await _unitOfWork.CompleteAsync();
		//}

		#endregion

		#region BlogImage Methods

		public async Task<BlogImageDto> GetBlogImageByIdAsync(string id)
		{
			var image = await _unitOfWork.BlogImageRepository.GetByIdAsync(id);
			return _mapper.Map<BlogImageDto>(image);
		}

		public async Task<IEnumerable<BlogImageDto>> GetImagesByBlogAsync(string blogId)
		{
			var images = await _unitOfWork.BlogImageRepository.GetAllAsync();
			return _mapper.Map<IEnumerable<BlogImageDto>>(
				images.Where(i => i.BlogId == blogId)
					  .OrderBy(i => i.DisplayOrder)
			);
		}

		public async Task<BlogImageDto> CreateBlogImageAsync(BlogImageDto imageDto)
		{

			var image = new BlogImage
			{
				Id = imageDto.Id,
				BlogId = imageDto.BlogId,
				ImageUrl = imageDto.ImageUrl,
				AltText = imageDto.AltText,
				Caption = imageDto.Caption,
				DisplayOrder = imageDto.DisplayOrder,
				IsPrimary = imageDto.IsPrimary,
				CreatedAt = DateTime.UtcNow,
				IsActive = true
			};

			await _unitOfWork.BlogImageRepository.AddAsync(image);
			await _unitOfWork.CompleteAsync();

			return new BlogImageDto(
				image.Id,
				image.BlogId,
				image.ImageUrl,
				image.AltText,
				image.Caption,
				image.DisplayOrder,
				image.IsPrimary,
				image.CreatedAt,
				image.UpdatedAt,
				image.IsActive
			);
		}

		public async Task UpdateBlogImageAsync(BlogImageDto imageDto)
		{
			var existingImage = await _unitOfWork.BlogImageRepository.GetByIdAsync(imageDto.Id);
			if (existingImage == null)
				throw new KeyNotFoundException($"Blog image with ID {imageDto.Id} not found");

			_mapper.Map(imageDto, existingImage);
			existingImage.UpdatedAt = DateTime.UtcNow;

			await _unitOfWork.BlogImageRepository.UpdateAsync(existingImage);
			await _unitOfWork.CompleteAsync();
		}

		public async Task DeleteBlogImageAsync(string id)
		{
			var image = await _unitOfWork.BlogImageRepository.GetByIdAsync(id);
			if (image == null)
				throw new KeyNotFoundException($"Blog image with ID {id} not found");

			await _unitOfWork.BlogImageRepository.DeleteAsync(image);
			await _unitOfWork.CompleteAsync();
		}

		#endregion
	}
}