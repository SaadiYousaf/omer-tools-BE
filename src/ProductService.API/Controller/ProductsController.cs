using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ProductService.Business.DTOs;
using ProductService.Business.Interfaces;

namespace ProductService.API.Controller
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductsController : ControllerBase
    {
        private readonly IProductService _productService;
        private readonly ILogger<ProductsController> _logger;

        public ProductsController(
            IProductService productService,
            ILogger<ProductsController> logger
        )
        {
            _productService = productService ?? throw new ArgumentNullException(nameof(productService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
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

                return Ok(products);
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
                return product == null ? NotFound() : Ok(product);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting full product details with ID {id}");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("featured")]
        public async Task<IActionResult> GetFeaturedProducts()
        {
            try
            {
                var featuredProducts = await _productService.GetFeaturedProductsAsync();
                return Ok(featuredProducts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting featured products");
                return StatusCode(500, "Internal server error");
            }
        }
        [HttpGet("redemption")]
        public async Task<IActionResult> GetRedemptionProducts()
        {
            try
            {
                var allProducts = await _productService.GetAllProductsAsync();
                var redemptionProducts = allProducts.Where(p => p.IsRedemption);
                return Ok(redemptionProducts);
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
}