using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ProductService.Business.DTOs;
using ProductService.Domain.Entites;
using ProductService.Domain.Entities;
using ProductService.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ProductService.API.Controller
{
    [ApiController]
    [Route("api/[controller]")]
    public class CategoriesController : ControllerBase
    {
        private readonly IRepository<Category> _categoryRepository;
        private readonly IRepository<Brand> _brandRepository;
        private readonly IRepository<Subcategory> _subcategoryRepository;
        private readonly IRepository<BrandCategory> _brandCategoryRepository;
        private readonly ILogger<CategoriesController> _logger;

        public CategoriesController(
            IRepository<Category> categoryRepository,
            IRepository<Brand> brandRepository,
            IRepository<Subcategory> subcategoryRepository,
            IRepository<BrandCategory> brandCategoryRepository,
            ILogger<CategoriesController> logger
        )
        {
            _categoryRepository = categoryRepository;
            _brandRepository = brandRepository;
            _subcategoryRepository = subcategoryRepository;
            _brandCategoryRepository = brandCategoryRepository;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> CreateCategory([FromBody] CategoryDto categoryDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Invalid model state for category creation");
                    return BadRequest(new
                    {
                        Status = "Validation Error",
                        Message = "Invalid request data",
                        Errors = ModelState.Values
                            .SelectMany(v => v.Errors)
                            .Select(e => e.ErrorMessage)
                    });
                }

                // Generate a unique ID if not provided
                var categoryId = !string.IsNullOrEmpty(categoryDto.Id) && categoryDto.Id != "0" ?
                    categoryDto.Id :
                    Guid.NewGuid().ToString();

                var category = new Category
                {
                    Id = categoryId,
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
                return StatusCode(500, new
                {
                    Status = "Error",
                    Message = "An error occurred while creating the category",
                    Detail = ex.Message,
                    InnerException = ex.InnerException?.Message
                });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetCategoryById(string id)
        {
            try
            {
                var category = await _categoryRepository.GetByIdAsync(id, "BrandCategories.Brand");
                if (category == null)
                {
                    _logger.LogWarning($"Category with ID {id} not found");
                    return NotFound(new { Message = $"Category with ID {id} not found" });
                }

                return Ok(
                    new
                    {
                        category.Id,
                        category.Name,
                        category.Description,
                        category.ImageUrl,
                        category.DisplayOrder,
                        category.CreatedAt,
                        Brands = category.BrandCategories.Select(bc => new
                        {
                            bc.Brand.Id,
                            bc.Brand.Name,
                            bc.Brand.LogoUrl
                        })
                    }
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting category with ID {id}");
                return StatusCode(500, new
                {
                    Status = "Error",
                    Message = "An error occurred while retrieving the category",
                    Detail = ex.Message
                });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAllCategories()
        {
            try
            {
                var categories = await _categoryRepository.GetAllAsync("BrandCategories.Brand");
                return Ok(
                    categories.Select(c => new
                    {
                        c.Id,
                        c.Name,
                        c.Description,
                        c.ImageUrl,
                        c.DisplayOrder,
                        BrandCount = c.BrandCategories.Count
                    })
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting categories");
                return StatusCode(500, new
                {
                    Status = "Error",
                    Message = "An error occurred while retrieving categories",
                    Detail = ex.Message
                });
            }
        }

        [HttpGet("{id}/brands")]
        public async Task<IActionResult> GetBrandsByCategory(string id)
        {
            try
            {
                var category = await _categoryRepository.GetByIdAsync(id);
                if (category == null)
                {
                    return NotFound(new { Message = $"Category with ID {id} not found" });
                }

                // Get brands through the BrandCategory join table
                var brandCategories = await _brandCategoryRepository.GetAsync(
                    bc => bc.CategoryId == id,
                    "Brand"
                );

                return Ok(brandCategories.Select(bc => new
                {
                    bc.Brand.Id,
                    bc.Brand.Name,
                    bc.Brand.Description,
                    bc.Brand.LogoUrl,
                    bc.Brand.WebsiteUrl
                }));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting brands for category ID {id}");
                return StatusCode(500, new
                {
                    Status = "Error",
                    Message = "An error occurred while retrieving brands",
                    Detail = ex.Message
                });
            }
        }

        [HttpGet("{id}/subcategories")]
        public async Task<IActionResult> GetSubcategoriesByCategory(string id)
        {
            try
            {
                var category = await _categoryRepository.GetByIdAsync(id);
                if (category == null)
                {
                    return NotFound(new { Message = $"Category with ID {id} not found" });
                }

                var subcategories = await _subcategoryRepository.GetAsync(s => s.CategoryId == id);
                return Ok(subcategories.Select(s => new
                {
                    s.Id,
                    s.Name,
                    s.Description,
                    s.ImageUrl,
                    s.DisplayOrder
                }));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting subcategories for category ID {id}");
                return StatusCode(500, new
                {
                    Status = "Error",
                    Message = "An error occurred while retrieving subcategories",
                    Detail = ex.Message
                });
            }
        }

        [HttpGet("{id}/full")]
        public async Task<IActionResult> GetCategoryFullDetails(string id)
        {
            try
            {
                var category = await _categoryRepository.GetByIdAsync(id, "BrandCategories.Brand");
                if (category == null)
                {
                    return NotFound(new { Message = $"Category with ID {id} not found" });
                }

                var subcategories = await _subcategoryRepository.GetAsync(s => s.CategoryId == id);

                return Ok(new
                {
                    Category = new
                    {
                        category.Id,
                        category.Name,
                        category.Description,
                        category.ImageUrl,
                        category.DisplayOrder
                    },
                    Brands = category.BrandCategories.Select(bc => new
                    {
                        bc.Brand.Id,
                        bc.Brand.Name,
                        bc.Brand.Description,
                        bc.Brand.LogoUrl,
                        bc.Brand.WebsiteUrl
                    }),
                    Subcategories = subcategories.Select(s => new
                    {
                        s.Id,
                        s.Name,
                        s.Description,
                        s.ImageUrl,
                        s.DisplayOrder
                    })
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting full details for category ID {id}");
                return StatusCode(500, new
                {
                    Status = "Error",
                    Message = "An error occurred while retrieving category details",
                    Detail = ex.Message
                });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCategory(string id, [FromBody] CategoryDto categoryDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new
                    {
                        Status = "Validation Error",
                        Message = "Invalid request data",
                        Errors = ModelState.Values
                            .SelectMany(v => v.Errors)
                            .Select(e => e.ErrorMessage)
                    });
                }

                var existingCategory = await _categoryRepository.GetByIdAsync(id);
                if (existingCategory == null)
                {
                    return NotFound(new { Message = $"Category with ID {id} not found" });
                }

                existingCategory.Name = categoryDto.Name?.Trim();
                existingCategory.Description = categoryDto.Description?.Trim();
                existingCategory.ImageUrl = categoryDto.ImageUrl?.Trim();
                existingCategory.DisplayOrder = categoryDto.DisplayOrder;
                existingCategory.UpdatedAt = DateTime.UtcNow;

                await _categoryRepository.UpdateAsync(existingCategory);
                await _categoryRepository.SaveChangesAsync();

                return Ok(new
                {
                    Message = "Category updated successfully",
                    Category = new
                    {
                        existingCategory.Id,
                        existingCategory.Name,
                        existingCategory.Description,
                        existingCategory.ImageUrl,
                        existingCategory.DisplayOrder
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating category with ID {id}");
                return StatusCode(500, new
                {
                    Status = "Error",
                    Message = "An error occurred while updating the category",
                    Detail = ex.Message
                });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCategory(string id)
        {
            try
            {
                var category = await _categoryRepository.GetByIdAsync(id);
                if (category == null)
                {
                    return NotFound(new { Message = $"Category with ID {id} not found" });
                }

                // Check if category has brands through the join table
                var hasBrands = await _brandCategoryRepository.ExistsAsync(bc => bc.CategoryId == id);
                if (hasBrands)
                {
                    return BadRequest(new { Message = "Cannot delete category with associated brands" });
                }

                // Check if category has subcategories
                var hasSubcategories = await _subcategoryRepository.ExistsAsync(s => s.CategoryId == id);
                if (hasSubcategories)
                {
                    return BadRequest(new { Message = "Cannot delete category with associated subcategories" });
                }

                await _categoryRepository.DeleteAsync(category);
                await _categoryRepository.SaveChangesAsync();

                return Ok(new { Message = "Category deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting category with ID {id}");
                return StatusCode(500, new
                {
                    Status = "Error",
                    Message = "An error occurred while deleting the category",
                    Detail = ex.Message
                });
            }
        }
    }
}