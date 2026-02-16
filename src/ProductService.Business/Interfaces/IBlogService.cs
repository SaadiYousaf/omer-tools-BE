using ProductService.Business.DTOs;
using ProductService.Domain.Entites;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProductService.Business.Interfaces
{
	public interface IBlogService
	{
		#region Blog Methods
		Task<BlogDto> GetBlogByIdAsync(string id);
		Task<BlogFullDto> GetBlogFullDetailsAsync(string id);
		Task<BlogFullDto> GetBlogBySlugAsync(string slug);
		Task<IEnumerable<BlogDto>> GetAllBlogsAsync();
		Task<PaginatedResult<BlogDto>> GetBlogsOptimizedAsync(OptimizedBlogQuery queryParams);
		Task<PaginatedResult<BlogDto>> GetBlogsByCategoryAsync(string categoryId, int page = 1, int pageSize = 10);
		Task<PaginatedResult<BlogDto>> GetBlogsByTagAsync(string tagId, int page = 1, int pageSize = 10);
		Task<IEnumerable<BlogDto>> GetFeaturedBlogsAsync(int count = 5);
		Task<IEnumerable<BlogDto>> GetRecentBlogsAsync(int count = 5);
		Task<BlogDto> CreateBlogAsync(BlogDto blogDto);
		Task UpdateBlogAsync(BlogDto blogDto);
		Task DeleteBlogAsync(string id);
		Task IncrementViewCountAsync(string id);
		#endregion

		#region BlogCategory Methods
		//Task<BlogCategoryDto> GetBlogCategoryByIdAsync(string id);
		//Task<IEnumerable<BlogCategoryDto>> GetAllBlogCategoriesAsync();
		//Task<BlogCategoryDto> CreateBlogCategoryAsync(BlogCategoryDto categoryDto);
		//Task UpdateBlogCategoryAsync(BlogCategoryDto categoryDto);
		//Task DeleteBlogCategoryAsync(string id);
		#endregion

		#region BlogTag Methods
		//Task<BlogTagDto> GetBlogTagByIdAsync(string id);
		//Task<IEnumerable<BlogTagDto>> GetAllBlogTagsAsync();
		//Task<BlogTagDto> CreateBlogTagAsync(BlogTagDto tagDto);
		//Task UpdateBlogTagAsync(BlogTagDto tagDto);
		//Task DeleteBlogTagAsync(string id);
		#endregion

		#region BlogImage Methods
		Task<BlogImageDto> GetBlogImageByIdAsync(string id);
		Task<IEnumerable<BlogImageDto>> GetImagesByBlogAsync(string blogId);
		Task<BlogImageDto> CreateBlogImageAsync(BlogImageDto imageDto);
		Task UpdateBlogImageAsync(BlogImageDto imageDto);
		Task DeleteBlogImageAsync(string id);
		#endregion
	}
}
