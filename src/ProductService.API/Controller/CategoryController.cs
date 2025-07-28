using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ProductService.Business.DTOs;
using ProductService.Domain.Entites;
using ProductService.Domain.Interfaces;

namespace ProductService.API.Controller
{
    [ApiController]
    [Route("api/[controller]")]
    public class CategoriesController : ControllerBase
    {
        private readonly IRepository<Category> _categoryRepository;
        private readonly ILogger<CategoriesController> _logger;
        private readonly IRepository<Brand> _brandRepository; // For validation

        public CategoriesController(
            IRepository<Category> categoryRepository,
            ILogger<CategoriesController> logger,
            IRepository<Brand> brandRepository
        )
        {
            _categoryRepository =
                categoryRepository ?? throw new ArgumentNullException(nameof(categoryRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _brandRepository =
                brandRepository ?? throw new ArgumentNullException(nameof(brandRepository));
        }

        [HttpPost]
        public async Task<IActionResult> CreateCategory([FromBody] CategoryDto categoryDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Invalid model state for category creation");
                    return BadRequest(ModelState);
                }

                // Validate Brand exists
                var brandExists = await _brandRepository.ExistsAsync(categoryDto.BrandId);
                if (!brandExists)
                {
                    return BadRequest($"Brand with ID {categoryDto.BrandId} does not exist");
                }

                var category = new Category
                {
                    BrandId = categoryDto.BrandId,
                    Name = categoryDto.Name?.Trim(),
                    Description = categoryDto.Description?.Trim(),
                    ImageUrl = categoryDto.ImageUrl?.Trim(),
                    DisplayOrder = categoryDto.DisplayOrder,
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true,
                };

                await _categoryRepository.AddAsync(category);
                await _categoryRepository.SaveChangesAsync();

                return CreatedAtAction(
                    nameof(GetCategoryById),
                    new { id = category.Id },
                    new
                    {
                        category.Id,
                        category.BrandId,
                        category.Name,
                        category.Description,
                        category.ImageUrl,
                        category.DisplayOrder,
                    }
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating category");
                return StatusCode(
                    500,
                    new
                    {
                        Status = "Error",
                        Message = "An error occurred while creating the category",
                        Detail = ex.Message,
                    }
                );
            }
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetCategoryById(int id)
        {
            try
            {
                var category = await _categoryRepository.GetByIdAsync(id);
                if (category == null)
                {
                    _logger.LogWarning($"Category with ID {id} not found");
                    return NotFound();
                }

                return Ok(
                    new
                    {
                        category.Id,
                        category.BrandId,
                        category.Name,
                        category.Description,
                        category.ImageUrl,
                        category.DisplayOrder,
                        category.CreatedAt,
                    }
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting category with ID {id}");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAllCategories([FromQuery] int? brandId = null)
        {
            try
            {
                IEnumerable<Category> categories;

                if (brandId.HasValue)
                {
                    categories = (await _categoryRepository.GetAllAsync()).Where(c =>
                        c.BrandId == brandId.Value
                    );
                }
                else
                {
                    categories = await _categoryRepository.GetAllAsync();
                }

                return Ok(
                    categories.Select(c => new
                    {
                        c.Id,
                        c.BrandId,
                        c.Name,
                        c.Description,
                        c.ImageUrl,
                        c.DisplayOrder,
                    })
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting categories");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateCategory(int id, [FromBody] CategoryDto categoryDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var existingCategory = await _categoryRepository.GetByIdAsync(id);
                if (existingCategory == null)
                {
                    return NotFound();
                }

                // Validate Brand exists if BrandId is being changed
                if (existingCategory.BrandId != categoryDto.BrandId)
                {
                    var brandExists = await _brandRepository.ExistsAsync(categoryDto.BrandId);
                    if (!brandExists)
                    {
                        return BadRequest($"Brand with ID {categoryDto.BrandId} does not exist");
                    }
                }

                existingCategory.BrandId = categoryDto.BrandId;
                existingCategory.Name = categoryDto.Name?.Trim();
                existingCategory.Description = categoryDto.Description?.Trim();
                existingCategory.ImageUrl = categoryDto.ImageUrl?.Trim();
                existingCategory.DisplayOrder = categoryDto.DisplayOrder;
                existingCategory.UpdatedAt = DateTime.UtcNow;

                await _categoryRepository.UpdateAsync(existingCategory);
                await _categoryRepository.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating category with ID {id}");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            try
            {
                var category = await _categoryRepository.GetByIdAsync(id);
                if (category == null)
                {
                    return NotFound();
                }

                await _categoryRepository.DeleteAsync(category);
                await _categoryRepository.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting category with ID {id}");
                return StatusCode(500, "Internal server error");
            }
        }
    }
}
