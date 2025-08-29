using Microsoft.AspNetCore.Mvc;
using ProductService.Business.DTOs;
using ProductService.Domain.Entites;
using ProductService.Domain.Entities;
using ProductService.Domain.Interfaces;

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
        private readonly IRepository<BrandImage> _brandImageRepository;
        private readonly ILogger<BrandsController> _logger;

        public BrandsController(
            IRepository<Brand> brandRepository,
            IRepository<Category> categoryRepository,
            IRepository<Product> productRepository,
            IRepository<BrandCategory> brandCategoryRepository,
            IRepository<BrandImage> brandImageRepository,
            ILogger<BrandsController> logger
        )
        {
            _brandRepository = brandRepository;
            _categoryRepository = categoryRepository;
            _productRepository = productRepository;
            _brandCategoryRepository = brandCategoryRepository;
            _brandImageRepository = brandImageRepository;
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
        public async Task<IActionResult> GetBrandById(string id, bool includeImages = false)
        {
            try
            {
                var brand = includeImages
                    ? await _brandRepository.GetByIdAsync(id, "BrandCategories.Category", "Images")
                    : await _brandRepository.GetByIdAsync(id, "BrandCategories.Category");

                if (brand == null)
                {
                    _logger.LogWarning($"Brand with ID {id} not found");
                    return NotFound();
                }

                var result = new
                {
                    brand.Id,
                    CategoryIds = brand.BrandCategories.Select(bc => bc.CategoryId).ToList(),
                    CategoryNames = brand.BrandCategories.Select(bc => bc.Category.Name).ToList(),
                    brand.Name,
                    brand.Description,
                    brand.WebsiteUrl,
                    brand.CreatedAt,
                    brand.UpdatedAt,
                    Images = includeImages ? brand.Images.Select(i => new
                    {
                        i.Id,
                        i.ImageUrl,
                        i.AltText,
                        i.DisplayOrder,
                        i.IsPrimary
                    }) : null
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting brand with ID {id}");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAllBrands([FromQuery] List<string> categoryIds = null, bool includeImages = false)
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

                    brands = await _brandRepository.GetAsync(
    b => brandIds.Contains(b.Id),
    includeImages ? new[] { "BrandCategories.Category", "Images" }
                  : new[] { "BrandCategories.Category" });

                }
                else
                {
                    

                    brands = await _brandRepository.GetAllAsync(
                        includeImages ? new[] { "BrandCategories.Category", "Images" }
                  : new[] { "BrandCategories.Category" });
                }

                return Ok(brands.Select(b => new
                {
                    b.Id,
                    CategoryIds = b.BrandCategories.Select(bc => bc.CategoryId).ToList(),
                    CategoryNames = b.BrandCategories.Select(bc => bc.Category.Name).ToList(),
                    b.Name,
                    b.Description,
                    b.WebsiteUrl,
                    Images = includeImages ? b.Images.Select(i => new
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

        [HttpPost("images")]
        public async Task<IActionResult> UploadBrandImage(
            [FromForm] IFormFile file,
            [FromForm] string brandId,
            [FromForm] string altText = "",
            [FromForm] int displayOrder = 0,
            [FromForm] bool isPrimary = false)
        {
            try
            {
                if (file == null || file.Length == 0)
                    return BadRequest(new { success = false, message = "No file uploaded" });

                if (string.IsNullOrEmpty(brandId))
                    return BadRequest(new { success = false, message = "Invalid brand ID" });

                // Validate file type
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
                var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (string.IsNullOrEmpty(extension) || !allowedExtensions.Contains(extension))
                    return BadRequest(new
                    {
                        success = false,
                        message = $"Invalid file type. Allowed: {string.Join(", ", allowedExtensions)}"
                    });

                // Check if brand exists
                var brand = await _brandRepository.GetByIdAsync(brandId);
                if (brand == null)
                    return NotFound(new { success = false, message = "Brand not found" });

                // Ensure directories exist
                var webRootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                Directory.CreateDirectory(webRootPath);

                var uploadsDir = Path.Combine(webRootPath, "uploads", "brands");
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
                var brandImage = new BrandImage
                {
                    Id = Guid.NewGuid().ToString(),
                    BrandId = brandId,
                    ImageUrl = $"/uploads/brands/{fileName}",
                    AltText = altText,
                    DisplayOrder = displayOrder,
                    IsPrimary = isPrimary,
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                };

                await _brandImageRepository.AddAsync(brandImage);
                await _brandImageRepository.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    image = new
                    {
                        brandImage.Id,
                        brandImage.ImageUrl,
                        brandImage.AltText,
                        brandImage.DisplayOrder,
                        brandImage.IsPrimary
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Brand image upload failed");
                return StatusCode(500, new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        [HttpGet("{id}/images")]
        public async Task<IActionResult> GetBrandImages(string id)
        {
            try
            {
                var images = await _brandImageRepository.GetAsync(ci => ci.BrandId == id && ci.IsActive);
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
                _logger.LogError(ex, $"Error getting images for brand ID {id}");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpDelete("images/{id}")]
        public async Task<IActionResult> DeleteBrandImage(string id)
        {
            try
            {
                var image = await _brandImageRepository.GetByIdAsync(id);
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

                await _brandImageRepository.DeleteAsync(image);
                await _brandImageRepository.SaveChangesAsync();

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

                // Delete brand images first
                var brandImages = await _brandImageRepository.GetAsync(bi => bi.BrandId == id);
                foreach (var image in brandImages)
                {
                    // Delete physical file
                    if (!string.IsNullOrEmpty(image.ImageUrl))
                    {
                        var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", image.ImageUrl.TrimStart('/'));
                        if (System.IO.File.Exists(filePath))
                        {
                            System.IO.File.Delete(filePath);
                        }
                    }
                    await _brandImageRepository.DeleteAsync(image);
                }

                // Delete brand category relationships
                var brandCategories = await _brandCategoryRepository.GetAsync(bc => bc.BrandId == id);
                foreach (var brandCategory in brandCategories)
                {
                    await _brandCategoryRepository.DeleteAsync(brandCategory);
                }

                await _brandRepository.DeleteAsync(brand);
                await _brandRepository.SaveChangesAsync();
                await _brandCategoryRepository.SaveChangesAsync();
                await _brandImageRepository.SaveChangesAsync();

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
