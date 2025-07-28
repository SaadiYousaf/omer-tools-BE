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
    public class SubcategoriesController : ControllerBase
    {
        private readonly IRepository<Subcategory> _subcategoryRepository;
        private readonly IRepository<Category> _categoryRepository;
        private readonly ILogger<SubcategoriesController> _logger;

        public SubcategoriesController(
            IRepository<Subcategory> subcategoryRepository,
            IRepository<Category> categoryRepository,
            ILogger<SubcategoriesController> logger
        )
        {
            _subcategoryRepository =
                subcategoryRepository
                ?? throw new ArgumentNullException(nameof(subcategoryRepository));
            _categoryRepository =
                categoryRepository ?? throw new ArgumentNullException(nameof(categoryRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
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
                    return BadRequest(
                        $"Category with ID {subcategoryDto.CategoryId} does not exist"
                    );
                }

                var subcategory = new Subcategory
                {
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
                return StatusCode(
                    500,
                    new
                    {
                        Status = "Error",
                        Message = "An error occurred while creating the subcategory",
                        Detail = ex.Message,
                    }
                );
            }
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetSubcategoryById(int id)
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
        public async Task<IActionResult> GetAllSubcategories([FromQuery] int? categoryId = null)
        {
            try
            {
                IEnumerable<Subcategory> subcategories;

                if (categoryId.HasValue)
                {
                    // Validate category exists
                    if (!await _categoryRepository.ExistsAsync(categoryId.Value))
                    {
                        return BadRequest($"Category with ID {categoryId.Value} does not exist");
                    }

                    subcategories = (await _subcategoryRepository.GetAllAsync())
                        .Where(s => s.CategoryId == categoryId.Value)
                        .OrderBy(s => s.DisplayOrder);
                }
                else
                {
                    subcategories = (await _subcategoryRepository.GetAllAsync()).OrderBy(s =>
                        s.DisplayOrder
                    );
                }

                return Ok(
                    subcategories.Select(s => new
                    {
                        s.Id,
                        s.CategoryId,
                        s.Name,
                        s.Description,
                        s.ImageUrl,
                        s.DisplayOrder,
                    })
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting subcategories");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateSubcategory(
            int id,
            [FromBody] SubcategoryDto subcategoryDto
        )
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var existingSubcategory = await _subcategoryRepository.GetByIdAsync(id);
                if (existingSubcategory == null)
                {
                    return NotFound();
                }

                // Validate Category exists if CategoryId is being changed
                if (existingSubcategory.CategoryId != subcategoryDto.CategoryId)
                {
                    if (!await _categoryRepository.ExistsAsync(subcategoryDto.CategoryId))
                    {
                        return BadRequest(
                            $"Category with ID {subcategoryDto.CategoryId} does not exist"
                        );
                    }
                }

                existingSubcategory.CategoryId = subcategoryDto.CategoryId;
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

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteSubcategory(int id)
        {
            try
            {
                var subcategory = await _subcategoryRepository.GetByIdAsync(id);
                if (subcategory == null)
                {
                    return NotFound();
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
