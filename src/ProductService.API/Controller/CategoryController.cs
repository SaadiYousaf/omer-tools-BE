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
        private readonly IRepository<CategoryImage> _categoryImageRepository;

        public CategoriesController(
            IRepository<Category> categoryRepository,
            IRepository<Brand> brandRepository,
            IRepository<Subcategory> subcategoryRepository,
            IRepository<BrandCategory> brandCategoryRepository,
             IRepository<CategoryImage> categoryImageRepository,
            ILogger<CategoriesController> logger
        )
        {
            _categoryRepository = categoryRepository;
            _brandRepository = brandRepository;
            _subcategoryRepository = subcategoryRepository;
            _brandCategoryRepository = brandCategoryRepository;
            _logger = logger;
            _categoryImageRepository = categoryImageRepository;
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
        [HttpGet]
        public async Task<IActionResult> GetAllCategories(bool includeImages = false)
        {
            try
            {
                // Determine which relationships to include based on the includeImages parameter
                var includeProperties = includeImages ?
                    new[] { "BrandCategories.Brand", "Images" } :
                    new[] { "BrandCategories.Brand" };

                var categories = await _categoryRepository.GetAllAsync(includeProperties);

                return Ok(
                    categories.Select(c => new
                    {
                        c.Id,
                        c.Name,
                        c.Description,
                        c.ImageUrl,
                        c.DisplayOrder,
                        BrandCount = c.BrandCategories.Count,
                        // Include images if requested
                        Images = includeImages ? c.Images.Select(img => new
                        {
                            img.Id,
                            img.ImageUrl,
                            img.AltText,
                            img.DisplayOrder,
                            img.IsPrimary
                        }) : null
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
        public async Task<IActionResult> GetBrandsByCategory(string id, bool includeImages = false)
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
                    includeImages ? "Brand.Images" : "Brand"
                );

                return Ok(brandCategories.Select(bc => new
                {
                    bc.Brand.Id,
                    bc.Brand.Name,
                    bc.Brand.Description,
                    bc.Brand.LogoUrl,
                    bc.Brand.WebsiteUrl,
                    // Include images if requested
                    Images = includeImages ? bc.Brand.Images.Select(i => new
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
                _logger.LogError(ex, $"Error getting brands for category ID {id}");
                return StatusCode(500, new
                {
                    Status = "Error",
                    Message = "An error occurred while retrieving brands",
                    Detail = ex.Message
                });
            }
        }

        [HttpGet("{id}/full")]
        public async Task<IActionResult> GetCategoryFullDetails(string id, bool includeBrandImages = false)
        {
            try
            {
                var category = await _categoryRepository.GetByIdAsync(id,
                    includeBrandImages ? "BrandCategories.Brand.Images" : "BrandCategories.Brand");

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
                        bc.Brand.WebsiteUrl,
                        // Include images if requested
                        Images = includeBrandImages ? bc.Brand.Images.Select(i => new
                        {
                            i.Id,
                            i.ImageUrl,
                            i.AltText,
                            i.DisplayOrder,
                            i.IsPrimary
                        }) : null
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
        //[HttpGet("{id}/brands")]
        //public async Task<IActionResult> GetBrandsByCategory(string id)
        //{
        //    try
        //    {
        //        var category = await _categoryRepository.GetByIdAsync(id);
        //        if (category == null)
        //        {
        //            return NotFound(new { Message = $"Category with ID {id} not found" });
        //        }

        //        // Get brands through the BrandCategory join table
        //        var brandCategories = await _brandCategoryRepository.GetAsync(
        //            bc => bc.CategoryId == id,
        //            "Brand"
        //        );

        //        return Ok(brandCategories.Select(bc => new
        //        {
        //            bc.Brand.Id,
        //            bc.Brand.Name,
        //            bc.Brand.Description,
        //            bc.Brand.LogoUrl,
        //            bc.Brand.WebsiteUrl
        //        }));
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, $"Error getting brands for category ID {id}");
        //        return StatusCode(500, new
        //        {
        //            Status = "Error",
        //            Message = "An error occurred while retrieving brands",
        //            Detail = ex.Message
        //        });
        //    }
        //}

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

        //[HttpGet("{id}/full")]
        //public async Task<IActionResult> GetCategoryFullDetails(string id)
        //{
        //    try
        //    {
        //        var category = await _categoryRepository.GetByIdAsync(id, "BrandCategories.Brand");
        //        if (category == null)
        //        {
        //            return NotFound(new { Message = $"Category with ID {id} not found" });
        //        }

        //        var subcategories = await _subcategoryRepository.GetAsync(s => s.CategoryId == id);

        //        return Ok(new
        //        {
        //            Category = new
        //            {
        //                category.Id,
        //                category.Name,
        //                category.Description,
        //                category.ImageUrl,
        //                category.DisplayOrder
        //            },
        //            Brands = category.BrandCategories.Select(bc => new
        //            {
        //                bc.Brand.Id,
        //                bc.Brand.Name,
        //                bc.Brand.Description,
        //                bc.Brand.LogoUrl,
        //                bc.Brand.WebsiteUrl
        //            }),
        //            Subcategories = subcategories.Select(s => new
        //            {
        //                s.Id,
        //                s.Name,
        //                s.Description,
        //                s.ImageUrl,
        //                s.DisplayOrder
        //            })
        //        });
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, $"Error getting full details for category ID {id}");
        //        return StatusCode(500, new
        //        {
        //            Status = "Error",
        //            Message = "An error occurred while retrieving category details",
        //            Detail = ex.Message
        //        });
        //    }
        //}

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
        [HttpPost("images")]
        public async Task<IActionResult> UploadCategoryImage(
    [FromForm] IFormFile file,
    [FromForm] string categoryId,
    [FromForm] string altText = "",
    [FromForm] int displayOrder = 0,
    [FromForm] bool isPrimary = false)
        {
            try
            {
                if (file == null || file.Length == 0)
                    return BadRequest(new { success = false, message = "No file uploaded" });

                if (string.IsNullOrEmpty(categoryId))
                    return BadRequest(new { success = false, message = "Invalid category ID" });

                // Validate file type
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
                var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (string.IsNullOrEmpty(extension) || !allowedExtensions.Contains(extension))
                    return BadRequest(new
                    {
                        success = false,
                        message = $"Invalid file type. Allowed: {string.Join(", ", allowedExtensions)}"
                    });

                // Check if category exists
                var category = await _categoryRepository.GetByIdAsync(categoryId);
                if (category == null)
                    return NotFound(new { success = false, message = "Category not found" });

                // Ensure directories exist
                var webRootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                Directory.CreateDirectory(webRootPath);

                var uploadsDir = Path.Combine(webRootPath, "uploads", "categories");
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
                var categoryImage = new CategoryImage
                {
                    Id = Guid.NewGuid().ToString(),
                    CategoryId = categoryId,
                    ImageUrl = $"/uploads/categories/{fileName}",
                    AltText = altText,
                    DisplayOrder = displayOrder,
                    IsPrimary = isPrimary,
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                };

                await _categoryImageRepository.AddAsync(categoryImage);
                await _categoryImageRepository.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    image = new
                    {
                        categoryImage.Id,
                        categoryImage.ImageUrl,
                        categoryImage.AltText,
                        categoryImage.DisplayOrder,
                        categoryImage.IsPrimary
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Category image upload failed");
                return StatusCode(500, new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        [HttpGet("{id}/images")]
        public async Task<IActionResult> GetCategoryImages(string id)
        {
            try
            {
                var images = await _categoryImageRepository.GetAsync(ci => ci.CategoryId == id && ci.IsActive);
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
                _logger.LogError(ex, $"Error getting images for category ID {id}");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpDelete("images/{id}")]
        public async Task<IActionResult> DeleteCategoryImage(string id)
        {
            try
            {
                var image = await _categoryImageRepository.GetByIdAsync(id);
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

                await _categoryImageRepository.DeleteAsync(image);
                await _categoryImageRepository.SaveChangesAsync();

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
    }
    public class CategoryImageDto
    {
        public string Id { get; set; }
        public string CategoryId { get; set; }
        public string ImageUrl { get; set; }
        public string AltText { get; set; }
        public int DisplayOrder { get; set; }
        public bool IsPrimary { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool IsActive { get; set; }
    }
}