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
    public class BrandsController : ControllerBase
    {
        private readonly IRepository<Brand> _brandRepository;
        private readonly ILogger<BrandsController> _logger;

        public BrandsController(
            IRepository<Brand> brandRepository,
            ILogger<BrandsController> logger
        )
        {
            _brandRepository =
                brandRepository ?? throw new ArgumentNullException(nameof(brandRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
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

                var brand = new Brand
                {
                    Name = brandDto.Name?.Trim(),
                    Description = brandDto.Description?.Trim(),
                    LogoUrl = brandDto.LogoUrl?.Trim(),
                    WebsiteUrl = brandDto.WebsiteUrl?.Trim(),
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true,
                };

                await _brandRepository.AddAsync(brand);
                await _brandRepository.SaveChangesAsync();

                return CreatedAtAction(
                    nameof(GetBrandById),
                    new { id = brand.Id },
                    new
                    {
                        brand.Id,
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
                return StatusCode(
                    500,
                    new
                    {
                        Status = "Error",
                        Message = "An error occurred while creating the brand",
                        Detail = ex.Message,
                    }
                );
            }
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetBrandById(int id)
        {
            try
            {
                var brand = await _brandRepository.GetByIdAsync(id);
                if (brand == null)
                {
                    _logger.LogWarning($"Brand with ID {id} not found");
                    return NotFound();
                }

                return Ok(
                    new
                    {
                        brand.Id,
                        brand.Name,
                        brand.Description,
                        brand.LogoUrl,
                        brand.WebsiteUrl,
                        brand.CreatedAt,
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
        public async Task<IActionResult> GetAllBrands()
        {
            try
            {
                var brands = await _brandRepository.GetAllAsync();
                return Ok(
                    brands.Select(b => new
                    {
                        b.Id,
                        b.Name,
                        b.Description,
                        b.LogoUrl,
                        b.WebsiteUrl,
                    })
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all brands");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateBrand(int id, [FromBody] BrandDto brandDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var existingBrand = await _brandRepository.GetByIdAsync(id);
                if (existingBrand == null)
                {
                    return NotFound();
                }

                existingBrand.Name = brandDto.Name?.Trim();
                existingBrand.Description = brandDto.Description?.Trim();
                existingBrand.LogoUrl = brandDto.LogoUrl?.Trim();
                existingBrand.WebsiteUrl = brandDto.WebsiteUrl?.Trim();
                existingBrand.UpdatedAt = DateTime.UtcNow;

                await _brandRepository.UpdateAsync(existingBrand);
                await _brandRepository.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating brand with ID {id}");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteBrand(int id)
        {
            try
            {
                var brand = await _brandRepository.GetByIdAsync(id);
                if (brand == null)
                {
                    return NotFound();
                }

                await _brandRepository.DeleteAsync(brand);
                await _brandRepository.SaveChangesAsync();

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
