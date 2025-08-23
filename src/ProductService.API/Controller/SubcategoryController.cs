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
    public class SubcategoriesController : ControllerBase
    {
        private readonly IRepository<Subcategory> _subcategoryRepository;
        private readonly IRepository<Category> _categoryRepository;
        private readonly IRepository<Product> _productRepository;
        private readonly ILogger<SubcategoriesController> _logger;

        public SubcategoriesController(
            IRepository<Subcategory> subcategoryRepository,
            IRepository<Category> categoryRepository,
            IRepository<Product> productRepository,
            ILogger<SubcategoriesController> logger
        )
        {
            _subcategoryRepository = subcategoryRepository;
            _categoryRepository = categoryRepository;
            _productRepository = productRepository;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> CreateSubcategory([FromBody] SubcategoryDto subcategoryDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Invalid model state for subcategory creation");
                    return BadRequest(ModelState);
                }

                // Validate Category exists
                if (!await _categoryRepository.ExistsAsync(subcategoryDto.CategoryId))
                {
                    return BadRequest($"Category with ID {subcategoryDto.CategoryId} not found");
                }

                var subcategory = new Subcategory
                {
                    Id = Guid.NewGuid().ToString(), // Generate unique ID
                    CategoryId = subcategoryDto.CategoryId,
                    Name = subcategoryDto.Name?.Trim(),
                    Description = subcategoryDto.Description?.Trim(),
                    ImageUrl = subcategoryDto.ImageUrl?.Trim(),
                    DisplayOrder = subcategoryDto.DisplayOrder,
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true,
                };

                await _subcategoryRepository.AddAsync(subcategory);
                await _subcategoryRepository.SaveChangesAsync();

                return CreatedAtAction(
                    nameof(GetSubcategoryById),
                    new { id = subcategory.Id },
                    new
                    {
                        subcategory.Id,
                        subcategory.CategoryId,
                        subcategory.Name,
                        subcategory.Description,
                        subcategory.ImageUrl,
                        subcategory.DisplayOrder,
                    }
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating subcategory");
                return StatusCode(500, new
                {
                    Status = "Error",
                    Message = "An error occurred while creating the subcategory",
                    Detail = ex.Message,
                });
            }
        }
        [HttpGet("{id}")]
        public async Task<IActionResult> GetSubcategoryById(string id)
        {
            try
            {
                var subcategory = await _subcategoryRepository.GetByIdAsync(id);
                if (subcategory == null)
                {
                    _logger.LogWarning($"Subcategory with ID {id} not found");
                    return NotFound();
                }

                return Ok(
                    new
                    {
                        subcategory.Id,
                        subcategory.CategoryId,
                        subcategory.Name,
                        subcategory.Description,
                        subcategory.ImageUrl,
                        subcategory.DisplayOrder,
                        subcategory.CreatedAt,
                        subcategory.UpdatedAt,
                    }
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting subcategory with ID {id}");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAllSubcategories([FromQuery] string categoryId = null)
        {
            try
            {
                IEnumerable<Subcategory> subcategories;

                if (!string.IsNullOrEmpty(categoryId))
                {
                    // Use repository filter for better performance
                    subcategories = await _subcategoryRepository.GetAsync(
                        s => s.CategoryId == categoryId
                    );
                }
                else
                {
                    subcategories = await _subcategoryRepository.GetAllAsync();
                }

                return Ok(subcategories.Select(s => new
                {
                    s.Id,
                    s.CategoryId,
                    s.Name,
                    s.Description,
                    s.ImageUrl,
                    s.DisplayOrder,
                }));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting subcategories");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("{id}/products")]
        public async Task<IActionResult> GetProductsBySubcategory(string id)
        {
            try
            {
                var subcategory = await _subcategoryRepository.GetByIdAsync(id);
                if (subcategory == null)
                {
                    return NotFound($"Subcategory with ID {id} not found");
                }

                var products = await _productRepository.GetAsync(p => p.SubcategoryId == id);
                return Ok(products.Select(p => new
                {
                    p.Id,
                    p.Name,
                    p.SKU,
                    p.Price
                }));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting products for subcategory ID {id}");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateSubcategory(
            string id,
            [FromBody] SubcategoryDto subcategoryDto
        )
        {
            try
            {
                if (!ModelState.IsValid) return BadRequest(ModelState);

                var existingSubcategory = await _subcategoryRepository.GetByIdAsync(id);
                if (existingSubcategory == null) return NotFound();

                // Only validate category if it's changing
                if (existingSubcategory.CategoryId != subcategoryDto.CategoryId)
                {
                    if (!await _categoryRepository.ExistsAsync(subcategoryDto.CategoryId))
                    {
                        return BadRequest($"Category with ID {subcategoryDto.CategoryId} not found");
                    }
                    existingSubcategory.CategoryId = subcategoryDto.CategoryId;
                }

                existingSubcategory.Name = subcategoryDto.Name?.Trim();
                existingSubcategory.Description = subcategoryDto.Description?.Trim();
                existingSubcategory.ImageUrl = subcategoryDto.ImageUrl?.Trim();
                existingSubcategory.DisplayOrder = subcategoryDto.DisplayOrder;
                existingSubcategory.UpdatedAt = DateTime.UtcNow;

                await _subcategoryRepository.UpdateAsync(existingSubcategory);
                await _subcategoryRepository.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating subcategory with ID {id}");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteSubcategory(string id)
        {
            try
            {
                var subcategory = await _subcategoryRepository.GetByIdAsync(id);
                if (subcategory == null) return NotFound();

                // Check for associated products
                var hasProducts = await _productRepository.ExistsAsync(p => p.SubcategoryId == id);
                if (hasProducts)
                {
                    return BadRequest("Cannot delete subcategory with associated products");
                }

                await _subcategoryRepository.DeleteAsync(subcategory);
                await _subcategoryRepository.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting subcategory with ID {id}");
                return StatusCode(500, "Internal server error");
            }
        }
    }
}