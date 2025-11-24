using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ProductService.Business.DTOs;
using ProductService.Business.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace ProductService.API.Controller
{
    [EnableCors("AllowAll")]
    [ApiController]
    [Route("api/[controller]")]
    public class ProductsController : ControllerBase
    {
        private readonly IProductService _productService;
		private readonly ISEOService _seoService;
		private readonly ILogger<ProductsController> _logger;
		private readonly IConfiguration _configuration;

		public ProductsController(
            IProductService productService,
			 ISEOService seoService,
			ILogger<ProductsController> logger,
			 IConfiguration configuration
		)
        {
            _productService = productService ?? throw new ArgumentNullException(nameof(productService));
			_seoService = seoService ?? throw new ArgumentNullException(nameof(seoService));
			_logger = logger ?? throw new ArgumentNullException(nameof(logger));
			_configuration = configuration;
		}

        [HttpGet]
        public async Task<IActionResult> GetAllProducts([FromQuery] ProductQueryParameters parameters)
        {
            try
            {
                IEnumerable<ProductDto> products;

                if (!string.IsNullOrEmpty(parameters.BrandId) && !string.IsNullOrEmpty(parameters.SubcategoryId))
                {
                    var brandProducts = await _productService.GetProductsByBrandAsync(parameters.BrandId);
                    products = brandProducts.Where(p => p.SubcategoryId == parameters.SubcategoryId);
                }
                else if (!string.IsNullOrEmpty(parameters.BrandId))
                {
                    products = await _productService.GetProductsByBrandAsync(parameters.BrandId);
                }
                else if (!string.IsNullOrEmpty(parameters.SubcategoryId))
                {
                    products = await _productService.GetProductsBySubcategoryAsync(parameters.SubcategoryId);
                }
                else
                {
                    products = await _productService.GetAllProductsAsync();
                }

                // Apply IsRedemption filter if provided
                if (parameters.IsRedemption.HasValue)
                {
                    products = products.Where(p => p.IsRedemption == parameters.IsRedemption.Value);
                }

				var totalCount = products.Count();

				if (parameters.Page.HasValue && parameters.Limit.HasValue)
				{
					var page = parameters.Page.Value;
					var limit = parameters.Limit.Value;
					var skip = (page - 1) * limit;

					products = products.Skip(skip).Take(limit);
				}

				var result = new PaginatedResponse<ProductDto>
				{
					Data = products.ToList(),
					Total = totalCount,
					Page = parameters.Page ?? 1,
					Limit = parameters.Limit ?? totalCount,
					TotalPages = parameters.Limit.HasValue ? (int)Math.Ceiling(totalCount / (double)parameters.Limit.Value) : 1
				};

				return Ok(result);
			}
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting products");
                return StatusCode(500, "Internal server error");
            }
        }
        [HttpGet("{id}")]
        public async Task<IActionResult> GetProductById(string id)
        {
            try
            {
                var product = await _productService.GetProductByIdAsync(id);
                return product == null ? NotFound() : Ok(product);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting product with ID {id}");
                return StatusCode(500, "Internal server error");
            }
        }



        [HttpGet("full/{id}")]
        public async Task<IActionResult> GetProductFullDetails(string id)
        {
            try
            {
                var product = await _productService.GetProductFullDetailsAsync(id);
				if (product == null)
					return NotFound();

				// Enhance with SEO data
				var baseUrl = _configuration["AppSettings:BaseUrl"] ?? $"{Request.Scheme}://{Request.Host}";
				var productWithSEO = _seoService.EnhanceProductWithSEO(product, baseUrl);

				return Ok(new
				{
					Product = productWithSEO,
					Brand = product.Brand,
					Subcategory = product.Subcategory,
					Category = product.Category,
					Images = product.Images,
					Variants = product.Variants
				});
			}
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting full product details with ID {id}");
                return StatusCode(500, "Internal server error");
            }
        }
		[HttpGet("full/{id}/seo")]
		public async Task<IActionResult> GetProductSEODetails(string id)
		{
			try
			{
				var product = await _productService.GetProductFullDetailsAsync(id);
				if (product == null)
					return NotFound();

				var baseUrl = _configuration["AppSettings:BaseUrl"] ?? $"{Request.Scheme}://{Request.Host}";
				var productWithSEO = _seoService.EnhanceProductWithSEO(product, baseUrl);

				// Return only SEO data for frontend meta tags
				return Ok(new
				{
					metaTitle = productWithSEO.MetaTitle,
					metaDescription = productWithSEO.MetaDescription,
					metaKeywords = productWithSEO.MetaKeywords,
					canonicalUrl = productWithSEO.CanonicalUrl,
					ogTitle = productWithSEO.OgTitle,
					ogDescription = productWithSEO.OgDescription,
					ogImage = productWithSEO.OgImage,
					productName = product.Product.Name,
					productDescription = product.Product.Description,
					productPrice = product.Product.Price,
					discountPrice = product.Product.DiscountPrice,
					brandName = product.Brand.Name,
					categoryName = product.Category.Name,
					subcategoryName = product.Subcategory.Name
				});
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Error getting SEO details for product with ID {id}");
				return StatusCode(500, "Internal server error");
			}
		}

		[HttpGet("seo/sitemap")]
		public async Task<IActionResult> GetProductsSitemapData()
		{
			try
			{
				var products = await _productService.GetAllProductsAsync();
				var baseUrl = _configuration["AppSettings:BaseUrl"] ?? $"{Request.Scheme}://{Request.Host}";

				var sitemapEntries = products.Select(p => new
				{
					url = $"{baseUrl.TrimEnd('/')}/products/{p.Id}",
					lastModified = p.UpdatedAt ?? p.CreatedAt,
					changeFrequency = "weekly",
					priority = p.IsFeatured ? 0.8 : 0.6
				}).ToList();

				return Ok(sitemapEntries);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error generating sitemap data");
				return StatusCode(500, "Internal server error");
			}
		}

		[HttpGet("full/{id}/structured-data")]
		public async Task<IActionResult> GetProductStructuredData(string id)
		{
			try
			{
				var product = await _productService.GetProductFullDetailsAsync(id);
				if (product == null)
					return NotFound();

				var baseUrl = _configuration["AppSettings:BaseUrl"] ?? $"{Request.Scheme}://{Request.Host}";
				var structuredData = _seoService.GenerateStructuredData(product, baseUrl);

				return Content(structuredData, "application/ld+json");
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Error generating structured data for product with ID {id}");
				return StatusCode(500, "Internal server error");
			}
		}


		[HttpGet("featured")]
        public async Task<IActionResult> GetFeaturedProducts([FromQuery] int? page = null, [FromQuery] int? limit = null)
        {
            try
            {
                var featuredProducts = await _productService.GetFeaturedProductsAsync();
				var totalCount = featuredProducts.Count();

				if (page.HasValue && limit.HasValue)
				{
					var skip = (page.Value - 1) * limit.Value;
					featuredProducts = featuredProducts.Skip(skip).Take(limit.Value);
				}
				var result = new PaginatedResponse<ProductDto>
				{
					Data = featuredProducts.ToList(),
					Total = totalCount,
					Page = page ?? 1,
					Limit = limit ?? totalCount,
					TotalPages = limit.HasValue ? (int)Math.Ceiling(totalCount / (double)limit.Value) : 1
				};

				return Ok(result);
			}
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting featured products");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("slider")]
        public async Task<IActionResult> GetProductSliderProducts([FromQuery] int? maxItems = null)
        {
            try
            {
                var products = await _productService.GetProductSliderProductsAsync(maxItems);
                return Ok(products);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while fetching slider products", error = ex.Message });
            }
        }
        [HttpGet("redemption")]
        public async Task<IActionResult> GetRedemptionProducts([FromQuery] int? page = null, [FromQuery] int? limit = null)
        {
            try
            {
                var allProducts = await _productService.GetAllProductsAsync();
                var redemptionProducts = allProducts.Where(p => p.IsRedemption);
				var totalCount = redemptionProducts.Count();
				var result = new PaginatedResponse<ProductDto>
				{
					Data = redemptionProducts.ToList(),
					Total = totalCount,
					Page = page ?? 1,
					Limit = limit ?? totalCount,
					TotalPages = limit.HasValue ? (int)Math.Ceiling(totalCount / (double)limit.Value) : 1
				};

				return Ok(result);
			}
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting redemption products");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateProduct([FromBody] ProductDto productDto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                // Create a new instance with the generated ID
                var productToCreate = new ProductDto
                {
                    Id = Guid.NewGuid().ToString(),
                    SubcategoryId = productDto.SubcategoryId,
                    BrandId = productDto.BrandId,
                    SKU = productDto.SKU,
                    Name = productDto.Name,
                    Description = productDto.Description,
                    Specifications = productDto.Specifications,
                    Price = productDto.Price,
                    IsRedemption = productDto.IsRedemption, // Add this line
                    DiscountPrice = productDto.DiscountPrice,
                    TagLine = productDto.TagLine,
                    StockQuantity = productDto.StockQuantity,
                    Weight = productDto.Weight,
                    Dimensions = productDto.Dimensions,
                    IsFeatured = productDto.IsFeatured,
                    WarrantyPeriod = productDto.WarrantyPeriod,
					// SEO Fields
					MetaTitle = productDto.MetaTitle,
					MetaDescription = productDto.MetaDescription,
					MetaKeywords = productDto.MetaKeywords,
					CanonicalUrl = productDto.CanonicalUrl,
					OgTitle = productDto.OgTitle,
					OgDescription = productDto.OgDescription,
					OgImage = productDto.OgImage,
					CreatedAt = DateTime.UtcNow,
                    IsActive = true,
                    Images = productDto.Images,
                    Variants = productDto.Variants
                };

                var createdProduct = await _productService.CreateProductAsync(productToCreate);
                return CreatedAtAction(
                    nameof(GetProductById),
                    new { id = createdProduct.Id },
                    createdProduct
                );
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid argument when creating product");
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating product");
                return StatusCode(500, "Internal server error");
            }
        }
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateProduct(string id, [FromBody] ProductDto productDto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                if (id != productDto.Id)
                    return BadRequest("ID mismatch");

				var existingProduct = await _productService.GetProductByIdAsync(id);
				if (existingProduct != null)
				{
					// If SEO fields are empty in the update, keep the existing ones
					productDto.MetaTitle = string.IsNullOrEmpty(productDto.MetaTitle) ? existingProduct.MetaTitle : productDto.MetaTitle;
					productDto.MetaDescription = string.IsNullOrEmpty(productDto.MetaDescription) ? existingProduct.MetaDescription : productDto.MetaDescription;
					productDto.MetaKeywords = string.IsNullOrEmpty(productDto.MetaKeywords) ? existingProduct.MetaKeywords : productDto.MetaKeywords;
					productDto.CanonicalUrl = string.IsNullOrEmpty(productDto.CanonicalUrl) ? existingProduct.CanonicalUrl : productDto.CanonicalUrl;
					productDto.OgTitle = string.IsNullOrEmpty(productDto.OgTitle) ? existingProduct.OgTitle : productDto.OgTitle;
					productDto.OgDescription = string.IsNullOrEmpty(productDto.OgDescription) ? existingProduct.OgDescription : productDto.OgDescription;
					productDto.OgImage = string.IsNullOrEmpty(productDto.OgImage) ? existingProduct.OgImage : productDto.OgImage;
				}

				productDto.UpdatedAt = DateTime.UtcNow;

				await _productService.UpdateProductAsync(productDto);
				return Ok(new { success = true, message = "Product updated successfully" });
			}
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, $"Product with ID {id} not found");
                return NotFound();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating product with ID {id}");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProduct(string id)
        {
            try
            {
                await _productService.DeleteProductAsync(id);
                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, $"Product with ID {id} not found");
                return NotFound();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting product with ID {id}");
                return StatusCode(500, "Internal server error");
            }
        }
        [HttpDelete("images/{imageId}")]
        public async Task<IActionResult> DeleteImage(string imageId)
        {
            try
            {
                if (string.IsNullOrEmpty(imageId))
                    return BadRequest(new { success = false, message = "Invalid image ID" });

                // Get the image first to check if it exists and get file path
                var image = await _productService.GetProductImageByIdAsync(imageId);
                if (image == null)
                    return NotFound(new { success = false, message = "Image not found" });

                // Delete the physical file
                if (!string.IsNullOrEmpty(image.ImageUrl))
                {
                    var webRootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                    var filePath = Path.Combine(webRootPath, image.ImageUrl.TrimStart('/'));

                    if (System.IO.File.Exists(filePath))
                    {
                        System.IO.File.Delete(filePath);
                        _logger.LogInformation($"Deleted physical file: {filePath}");
                    }
                }

                // Delete the image record from database
                await _productService.DeleteProductImageAsync(imageId);

                return Ok(new
                {
                    success = true,
                    message = "Image deleted successfully",
                    deletedImageId = imageId
                });
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, $"Image with ID {imageId} not found");
                return NotFound(new { success = false, message = "Image not found" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting image with ID {imageId}");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Internal server error while deleting image"
                });
            }
        }
        [HttpPost("images")]
        public async Task<IActionResult> UploadImage(
            [FromForm] IFormFile file,
            [FromForm] string productId,
            [FromForm] string altText = "",
            [FromForm] int displayOrder = 0,
            [FromForm] bool isPrimary = false)
        {
            try
            {
                if (file == null || file.Length == 0)
                    return BadRequest(new { success = false, message = "No file uploaded" });

                if (string.IsNullOrEmpty(productId))
                    return BadRequest(new { success = false, message = "Invalid product ID" });

                // Validate file type
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
                var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (string.IsNullOrEmpty(extension) || !allowedExtensions.Contains(extension))
                    return BadRequest(new
                    {
                        success = false,
                        message = $"Invalid file type. Allowed: {string.Join(", ", allowedExtensions)}"
                    });

                // Ensure directories exist
                var webRootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                Directory.CreateDirectory(webRootPath);

                var uploadsDir = Path.Combine(webRootPath, "uploads");
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
                var imageDto = new ProductImageDto(
                    Id: Guid.NewGuid().ToString(),
                    ProductId: productId,
                    ImageUrl: $"/uploads/{fileName}",
                    AltText: altText,
                    DisplayOrder: displayOrder,
                    IsPrimary: isPrimary,
                    CreatedAt: DateTime.UtcNow,
                    UpdatedAt: null,
                    IsActive: true
                );

                var createdImage = await _productService.CreateProductImageAsync(imageDto);

                return Ok(new { success = true, image = createdImage });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Image upload failed");
                return StatusCode(500, new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }
    }
}
public class ProductQueryParameters
{
    public string? BrandId { get; set; }
    public string? SubcategoryId { get; set; }
    public bool? IsRedemption { get; set; }

	public int? Page { get; set; }
	public int? Limit { get; set; }
}
public class PaginatedResponse<T>
{
	public List<T> Data { get; set; } = new List<T>();
	public int Total { get; set; }
	public int Page { get; set; }
	public int Limit { get; set; }
	public int TotalPages { get; set; }
}