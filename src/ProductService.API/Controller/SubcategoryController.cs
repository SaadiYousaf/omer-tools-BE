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
        private readonly IRepository<SubcategoryImage> _subcategoryImageRepository;

        public SubcategoriesController(
            IRepository<Subcategory> subcategoryRepository,
            IRepository<Category> categoryRepository,
            IRepository<Product> productRepository,
            IRepository<SubcategoryImage> subcategoryImageRepository,
            ILogger<SubcategoriesController> logger
        )
        {
            _subcategoryRepository = subcategoryRepository;
            _categoryRepository = categoryRepository;
            _productRepository = productRepository;
            _logger = logger;
            _subcategoryImageRepository = subcategoryImageRepository;
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
        public async Task<IActionResult> GetAllSubcategories([FromQuery] bool includeImages = false ,string categoryId = null)
        {
            try
            {
                IEnumerable<Subcategory> subcategories;

                if (!string.IsNullOrEmpty(categoryId))
                {
                    // Use repository filter for better performance
                    var allSubcategories = await _subcategoryRepository.GetAllAsync(s => s.Images);
                    subcategories = allSubcategories.Where(s => s.CategoryId == categoryId);

                }
                else
                {
                    subcategories = await _subcategoryRepository.GetAllAsync("Images");
                }

                return Ok(subcategories.Select(s => new
                {
                    s.Id,
                    s.CategoryId,
                    s.Name,
                    s.Description,
                    s.ImageUrl,
                    s.DisplayOrder,
                    Images = includeImages ? s.Images.Select(i => new
                    {
                        i.Id,
                        i.ImageUrl,
                        i.AltText,
                        i.DisplayOrder,
                        i.IsPrimary
                    }) : null
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
        [HttpPost("images")]
        public async Task<IActionResult> UploadSubcategoryImage(
    [FromForm] IFormFile file,
    [FromForm] string subcategoryId,
    [FromForm] string altText = "",
    [FromForm] int displayOrder = 0,
    [FromForm] bool isPrimary = false)
        {
            try
            {
                if (file == null || file.Length == 0)
                    return BadRequest(new { success = false, message = "No file uploaded" });

                if (string.IsNullOrEmpty(subcategoryId))
                    return BadRequest(new { success = false, message = "Invalid subcategory ID" });

                // Validate file type
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
                var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (string.IsNullOrEmpty(extension) || !allowedExtensions.Contains(extension))
                    return BadRequest(new
                    {
                        success = false,
                        message = $"Invalid file type. Allowed: {string.Join(", ", allowedExtensions)}"
                    });

                // Check if subcategory exists
                var subcategory = await _subcategoryRepository.GetByIdAsync(subcategoryId);
                if (subcategory == null)
                    return NotFound(new { success = false, message = "Subcategory not found" });

                // Ensure directories exist
                var webRootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                Directory.CreateDirectory(webRootPath);

                var uploadsDir = Path.Combine(webRootPath, "uploads", "subcategories");
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
                var subcategoryImage = new SubcategoryImage
                {
                    Id = Guid.NewGuid().ToString(),
                    SubcategoryId = subcategoryId,
                    ImageUrl = $"/uploads/subcategories/{fileName}",
                    AltText = altText,
                    DisplayOrder = displayOrder,
                    IsPrimary = isPrimary,
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                };

                await _subcategoryImageRepository.AddAsync(subcategoryImage);
                await _subcategoryImageRepository.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    image = new
                    {
                        subcategoryImage.Id,
                        subcategoryImage.ImageUrl,
                        subcategoryImage.AltText,
                        subcategoryImage.DisplayOrder,
                        subcategoryImage.IsPrimary
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Subcategory image upload failed");
                return StatusCode(500, new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        [HttpGet("{id}/images")]
        public async Task<IActionResult> GetSubcategoryImages(string id)
        {
            try
            {
                var images = await _subcategoryImageRepository.GetAsync(ci => ci.SubcategoryId == id && ci.IsActive);
                return Ok(images.Select(img => new
                {
                    img.Id,
                    img.ImageUrl,
                    img.AltText,
                    img.DisplayOrder,
                    img.IsPrimary
                }));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting images for subcategory ID {id}");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpDelete("images/{id}")]
        public async Task<IActionResult> DeleteSubcategoryImage(string id)
        {
            try
            {
                var image = await _subcategoryImageRepository.GetByIdAsync(id);
                if (image == null)
                    return NotFound(new { success = false, message = "Image not found" });

                // Delete physical file
                if (!string.IsNullOrEmpty(image.ImageUrl))
                {
                    var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", image.ImageUrl.TrimStart('/'));
                    if (System.IO.File.Exists(filePath))
                    {
                        System.IO.File.Delete(filePath);
                    }
                }

                await _subcategoryImageRepository.DeleteAsync(image);
                await _subcategoryImageRepository.SaveChangesAsync();

                return Ok(new { success = true, message = "Image deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting image with ID {id}");
                return StatusCode(500, new
                {
                    success = false,
                    message = ex.Message
                });
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