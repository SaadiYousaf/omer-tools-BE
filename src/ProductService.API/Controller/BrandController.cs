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
    public class BrandsController : ControllerBase
    {
        private readonly IRepository<Brand> _brandRepository;
        private readonly IRepository<Category> _categoryRepository;
        private readonly IRepository<Product> _productRepository;
        private readonly IRepository<BrandCategory> _brandCategoryRepository;
        private readonly ILogger<BrandsController> _logger;

        public BrandsController(
            IRepository<Brand> brandRepository,
            IRepository<Category> categoryRepository,
            IRepository<Product> productRepository,
            IRepository<BrandCategory> brandCategoryRepository,
            ILogger<BrandsController> logger
        )
        {
            _brandRepository = brandRepository;
            _categoryRepository = categoryRepository;
            _productRepository = productRepository;
            _brandCategoryRepository = brandCategoryRepository;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> CreateBrand([FromBody] BrandDto brandDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Invalid model state for brand creation");
                    return BadRequest(ModelState);
                }

                // Validate all categories exist
                foreach (var categoryId in brandDto.CategoryIds)
                {
                    if (!await _categoryRepository.ExistsAsync(categoryId))
                    {
                        return BadRequest($"Category with ID {categoryId} not found");
                    }
                }

                var brand = new Brand
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = brandDto.Name?.Trim(),
                    Description = brandDto.Description?.Trim(),
                    LogoUrl = brandDto.LogoUrl?.Trim(),
                    WebsiteUrl = brandDto.WebsiteUrl?.Trim(),
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true,
                };

                await _brandRepository.AddAsync(brand);

                // Add category relationships
                foreach (var categoryId in brandDto.CategoryIds)
                {
                    var brandCategory = new BrandCategory
                    {
                        BrandId = brand.Id,
                        CategoryId = categoryId
                    };
                    await _brandCategoryRepository.AddAsync(brandCategory);
                }

                await _brandRepository.SaveChangesAsync();
                await _brandCategoryRepository.SaveChangesAsync();

                return CreatedAtAction(
                    nameof(GetBrandById),
                    new { id = brand.Id },
                    new
                    {
                        brand.Id,
                        CategoryIds = brandDto.CategoryIds,
                        brand.Name,
                        brand.Description,
                        brand.LogoUrl,
                        brand.WebsiteUrl,
                    }
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating brand");
                return StatusCode(500, new
                {
                    Status = "Error",
                    Message = "An error occurred while creating the brand",
                    Detail = ex.Message,
                });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetBrandById(string id)
        {
            try
            {
                var brand = await _brandRepository.GetByIdAsync(id, "BrandCategories.Category");
                if (brand == null)
                {
                    _logger.LogWarning($"Brand with ID {id} not found");
                    return NotFound();
                }

                return Ok(
                    new
                    {
                        brand.Id,
                        CategoryIds = brand.BrandCategories.Select(bc => bc.CategoryId).ToList(),
                        CategoryNames = brand.BrandCategories.Select(bc => bc.Category.Name).ToList(),
                        brand.Name,
                        brand.Description,
                        brand.LogoUrl,
                        brand.WebsiteUrl,
                        brand.CreatedAt,
                        brand.UpdatedAt,
                    }
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting brand with ID {id}");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAllBrands([FromQuery] List<string> categoryIds = null)
        {
            try
            {
                IEnumerable<Brand> brands;

                if (categoryIds != null && categoryIds.Any())
                {
                    // Validate all categories exist
                    foreach (var categoryId in categoryIds)
                    {
                        if (!await _categoryRepository.ExistsAsync(categoryId))
                        {
                            return BadRequest($"Category with ID {categoryId} not found");
                        }
                    }

                    // Get brands that have all the specified categories
                    var brandIds = (await _brandCategoryRepository.GetAsync(bc => categoryIds.Contains(bc.CategoryId)))
                        .GroupBy(bc => bc.BrandId)
                        .Where(g => g.Count() == categoryIds.Count)
                        .Select(g => g.Key);

                    brands = await _brandRepository.GetAsync(b => brandIds.Contains(b.Id), "BrandCategories.Category");
                }
                else
                {
                    brands = await _brandRepository.GetAllAsync("BrandCategories.Category");
                }

                return Ok(brands.Select(b => new
                {
                    b.Id,
                    CategoryIds = b.BrandCategories.Select(bc => bc.CategoryId).ToList(),
                    CategoryNames = b.BrandCategories.Select(bc => bc.Category.Name).ToList(),
                    b.Name,
                    b.Description,
                    b.LogoUrl,
                    b.WebsiteUrl,
                }));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting brands");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateBrand(string id, [FromBody] BrandDto brandDto)
        {
            try
            {
                if (!ModelState.IsValid) return BadRequest(ModelState);

                var existingBrand = await _brandRepository.GetByIdAsync(id, "BrandCategories");
                if (existingBrand == null) return NotFound();

                // Validate all categories exist
                foreach (var categoryId in brandDto.CategoryIds)
                {
                    if (!await _categoryRepository.ExistsAsync(categoryId))
                    {
                        return BadRequest($"Category with ID {categoryId} not found");
                    }
                }

                // Update brand properties
                existingBrand.Name = brandDto.Name?.Trim();
                existingBrand.Description = brandDto.Description?.Trim();
                existingBrand.LogoUrl = brandDto.LogoUrl?.Trim();
                existingBrand.WebsiteUrl = brandDto.WebsiteUrl?.Trim();
                existingBrand.UpdatedAt = DateTime.UtcNow;

                // Update categories
                var currentCategoryIds = existingBrand.BrandCategories.Select(bc => bc.CategoryId).ToList();
                var newCategoryIds = brandDto.CategoryIds;

                // Remove categories that are no longer selected
                var categoriesToRemove = existingBrand.BrandCategories
                    .Where(bc => !newCategoryIds.Contains(bc.CategoryId))
                    .ToList();

                foreach (var categoryToRemove in categoriesToRemove)
                {
                    await _brandCategoryRepository.DeleteAsync(categoryToRemove);
                }

                // Add new categories
                var categoriesToAdd = newCategoryIds
                    .Where(cid => !currentCategoryIds.Contains(cid))
                    .Select(cid => new BrandCategory
                    {
                        BrandId = id,
                        CategoryId = cid
                    })
                    .ToList();

                foreach (var categoryToAdd in categoriesToAdd)
                {
                    await _brandCategoryRepository.AddAsync(categoryToAdd);
                }

                await _brandRepository.UpdateAsync(existingBrand);
                await _brandRepository.SaveChangesAsync();
                await _brandCategoryRepository.SaveChangesAsync();

                return Ok(new
                {
                    Message = "Brand updated successfully",
                    Brand = new
                    {
                        existingBrand.Id,
                        CategoryIds = brandDto.CategoryIds,
                        existingBrand.Name,
                        existingBrand.Description,
                        existingBrand.LogoUrl,
                        existingBrand.WebsiteUrl
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating brand with ID {id}");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteBrand(string id)
        {
            try
            {
                var brand = await _brandRepository.GetByIdAsync(id);
                if (brand == null) return NotFound();

                // Check for associated products
                var hasProducts = await _productRepository.ExistsAsync(p => p.BrandId == id);
                if (hasProducts)
                {
                    return BadRequest("Cannot delete brand with associated products");
                }

                // Delete brand category relationships first
                var brandCategories = await _brandCategoryRepository.GetAsync(bc => bc.BrandId == id);
                foreach (var brandCategory in brandCategories)
                {
                    await _brandCategoryRepository.DeleteAsync(brandCategory);
                }

                await _brandRepository.DeleteAsync(brand);
                await _brandRepository.SaveChangesAsync();
                await _brandCategoryRepository.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting brand with ID {id}");
                return StatusCode(500, "Internal server error");
            }
        }
    }
}