using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
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
            _productService =
                productService ?? throw new ArgumentNullException(nameof(productService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [HttpGet]

        public async Task<IActionResult> GetAllProducts([FromQuery] int? subcategoryId)
        {
            try
            {
                if (subcategoryId.HasValue)
                {
                    var filteredProducts = await _productService.GetProductsBySubcategoryAsync(subcategoryId.Value);
                    return Ok(filteredProducts);
                }

                var allProducts = await _productService.GetAllProductsAsync();
                return Ok(allProducts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting products");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetProductById(int id)
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

        [HttpGet("full/{id:int}")]
        public async Task<IActionResult> GetProductFullDetails(int id)
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

        [HttpPost]
        public async Task<IActionResult> CreateProduct([FromBody] ProductDto productDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var createdProduct = await _productService.CreateProductAsync(productDto);
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

        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateProduct(int id, [FromBody] ProductDto productDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                if (id != productDto.Id)
                {
                    return BadRequest("ID mismatch");
                }

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


        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            try
            {
                await _productService.DeleteProductAsync(id);
                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, $"Product with ID {id} not found");
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
     [FromForm] int productId,
     [FromForm] string altText = "",
     [FromForm] bool isPrimary = false)
        {
            try
            {
                // Validate input
                if (file == null || file.Length == 0)
                    return BadRequest(new { success = false, message = "No file uploaded" });

                if (productId <= 0)
                    return BadRequest(new { success = false, message = "Invalid product ID" });

                // Validate file type
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (string.IsNullOrEmpty(extension) || !allowedExtensions.Contains(extension))
                    return BadRequest(new
                    {
                        success = false,
                        message = $"Invalid file type. Allowed: {string.Join(", ", allowedExtensions)}"
                    });

                // Ensure wwwroot exists
                var webRootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                if (!Directory.Exists(webRootPath))
                {
                    Directory.CreateDirectory(webRootPath);
                    _logger.LogInformation("Created wwwroot directory");
                }

                // Create uploads directory if not exists
                var uploadsDir = Path.Combine(webRootPath, "uploads");
                if (!Directory.Exists(uploadsDir))
                {
                    Directory.CreateDirectory(uploadsDir);
                    _logger.LogInformation("Created uploads directory");
                }

                // Generate unique filename
                var fileName = $"{Guid.NewGuid()}{extension}";
                var filePath = Path.Combine(uploadsDir, fileName);

                // Save file with proper disposal
                _logger.LogInformation($"Attempting to save file to: {filePath}");
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                    await stream.FlushAsync();
                }

                // Verify file was created
                if (!System.IO.File.Exists(filePath))
                {
                    _logger.LogError($"File was not created at: {filePath}");
                    return StatusCode(500, new { success = false, message = "Failed to save file" });
                }

                _logger.LogInformation($"File successfully saved to: {filePath}");

                // Create image record
                var imageDto = new ProductImageDto(
                    Id: 0,
                    ProductId: productId,
                    ImageUrl: $"/uploads/{fileName}",
                    AltText: altText,
                    DisplayOrder: 0,
                    IsPrimary: isPrimary,
                    CreatedAt: DateTime.UtcNow,
                    UpdatedAt: null,
                    IsActive: true
                );

                var createdImage = await _productService.CreateProductImageAsync(imageDto);

                return Ok(new
                {
                    success = true,
                    image = createdImage,
                    filePath // For debugging
                });
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