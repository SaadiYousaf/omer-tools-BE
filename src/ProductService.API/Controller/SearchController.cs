using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ProductService.DataAccess.Data;
using ProductService.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ProductService.API.Controllers
{
    [EnableCors("AllowAll")]
    [ApiController]
    [Route("api/[controller]")]
    public class SearchController : ControllerBase
    {
        private readonly ProductDbContext _context;
        private readonly ILogger<SearchController> _logger;
        private const int AutocompleteLimit = 10;
        private const int SearchResultLimit = 50;

        public SearchController(
            ProductDbContext context,
            ILogger<SearchController> logger)
        {
            _context = context;
            _logger = logger;
        }
        private IActionResult ValidateSearchQuery(SearchQueryDto query)
        {
            if (string.IsNullOrWhiteSpace(query.Term))
                return BadRequest("Search term is required.");

            if (query.Page < 1)
                return BadRequest("Page must be greater than 0.");

            if (query.PageSize < 1 || query.PageSize > 100)
                return BadRequest("PageSize must be between 1 and 100.");

            return null;
        }
        [HttpGet("autocomplete")]
        public async Task<IActionResult> Autocomplete([FromQuery] string term)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(term) || term.Length < 2)
                {
                    return Ok(new List<AutocompleteSuggestionDto>());
                }

                var normalizedTerm = term.Trim().ToLower();

                var searchTerms = normalizedTerm.Split(' ', StringSplitOptions.RemoveEmptyEntries);

                if (searchTerms.Length == 0)
                {
                    return Ok(new List<AutocompleteSuggestionDto>());
                }

                // FIRST: Look for exact phrase matches (highest priority)
                var exactProductMatches = await _context.Products
                    .Include(p => p.Subcategory)
                        .ThenInclude(s => s.Category)
                    .Where(p => p.Name.ToLower().Contains(normalizedTerm))
                    .OrderByDescending(p => p.IsFeatured)
                    .ThenBy(p => p.Name)
                    .Select(p => new AutocompleteSuggestionDto
                    {
                        Type = "Product",
                        Value = p.Name,
                        Category = p.Subcategory.Category.Name + " > " + p.Subcategory.Name,
                        ReferenceId = p.Id
                    })
                    .Take(5) // Reserve some slots for exact matches
                    .ToListAsync();

                // SECOND: Look for products that match ALL search terms
                var allTermsProductMatches = await _context.Products
                    .Include(p => p.Subcategory)
                        .ThenInclude(s => s.Category)
                    .Where(p => searchTerms.All(st => p.Name.ToLower().Contains(st)))
                    .OrderByDescending(p => p.IsFeatured)
                    .ThenBy(p => p.Name)
                    .Select(p => new AutocompleteSuggestionDto
                    {
                        Type = "Product",
                        Value = p.Name,
                        Category = p.Subcategory.Category.Name + " > " + p.Subcategory.Name,
                        ReferenceId = p.Id
                    })
                    .Take(5) // Reserve slots for "all terms" matches
                    .ToListAsync();

                // THIRD: Look for products that match ANY search term (fallback)
                var remainingSlots = AutocompleteLimit - exactProductMatches.Count - allTermsProductMatches.Count;
                var anyTermProductMatches = new List<AutocompleteSuggestionDto>();

                if (remainingSlots > 0)
                {
                    anyTermProductMatches = await _context.Products
                        .Include(p => p.Subcategory)
                            .ThenInclude(s => s.Category)
                        .Where(p => searchTerms.Any(st => p.Name.ToLower().Contains(st)))
                        .OrderByDescending(p => p.IsFeatured)
                        .ThenBy(p => p.Name)
                        .Select(p => new AutocompleteSuggestionDto
                        {
                            Type = "Product",
                            Value = p.Name,
                            Category = p.Subcategory.Category.Name + " > " + p.Subcategory.Name,
                            ReferenceId = p.Id
                        })
                        .Take(remainingSlots)
                        .ToListAsync();
                }

                // Combine with priority: Exact matches > All terms matches > Any term matches
                var productMatches = exactProductMatches
                    .Concat(allTermsProductMatches)
                    .Concat(anyTermProductMatches)
                    .GroupBy(p => p.ReferenceId)
                    .Select(g => g.First())
                    .Take(AutocompleteLimit)
                    .ToList();

                // Brands search - match ANY of the search terms
                var brandMatches = await _context.Brands
                    .Include(b => b.BrandCategories)
                        .ThenInclude(bc => bc.Category)
                    .Where(b => searchTerms.Any(st => b.Name.ToLower().Contains(st)))
                    .Select(b => new AutocompleteSuggestionDto
                    {
                        ReferenceId = b.Id,
                        Value = b.Name,
                        Type = "Brand",
                        Category = b.BrandCategories.Select(bc => bc.Category.Name).FirstOrDefault() ?? "No Category"
                    })
                    .Take(AutocompleteLimit)
                    .ToListAsync();

                // Categories search - match ANY of the search terms
                var categoryMatches = await _context.Categories
                    .Where(c => searchTerms.Any(st => c.Name.ToLower().Contains(st)))
                    .Select(c => new AutocompleteSuggestionDto
                    {
                        Type = "Category",
                        Value = c.Name,
                        ReferenceId = c.Id
                    })
                    .Take(AutocompleteLimit)
                    .ToListAsync();

                // Merge results and remove duplicates
                var suggestions = productMatches
                    .Concat(brandMatches)
                    .Concat(categoryMatches)
                    .GroupBy(s => new { s.Type, s.Value })
                    .Select(g => g.First())
                    .Take(AutocompleteLimit)
                    .ToList();

                var scoredSuggestions = suggestions
    .Select(s => new
    {
        Suggestion = s,
        Score = searchTerms.Count(st => s.Value.ToLower().Contains(st)) +
                searchTerms.Count(st => s.Category?.ToLower().Contains(st) ?? false)
    })
    .Where(x => x.Score > 0) // Ensure at least one term matches
    .OrderByDescending(x => x.Score)
    .ThenBy(x => x.Suggestion.Value)
    .Select(x => x.Suggestion)
    .Take(AutocompleteLimit)
    .ToList();

                return Ok(scoredSuggestions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during autocomplete search");
                return StatusCode(500, "Internal server error");
            }
        }
        [HttpGet]
        public async Task<IActionResult> SearchProducts([FromQuery] SearchQueryDto query)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(query.Term))
                {
                    return BadRequest("Search term is required.");
                }
                var validationResult = ValidateSearchQuery(query);
                if (validationResult != null) return validationResult;

                var term = query.Term.Trim().ToLower();
                var searchTerms = term.Split(' ', StringSplitOptions.RemoveEmptyEntries);

                if (searchTerms.Length == 0)
                {
                    return BadRequest("Valid search term is required.");
                }

                var baseQuery = _context.Products
                    .Include(p => p.Brand)
                    .Include(p => p.Subcategory)
                        .ThenInclude(s => s.Category)
                    .Include(p => p.Images)
                    .AsQueryable();

                // Multi-term search using string concatenation
                baseQuery = baseQuery.Where(p =>
                    searchTerms.Any(st => EF.Functions.Like(p.Name, "%" + st + "%")) ||
                    //searchTerms.Any(st => EF.Functions.Like(p.Description, "%" + st + "%")) ||
                    //searchTerms.Any(st => EF.Functions.Like(p.Specifications, "%" + st + "%")) ||
                    searchTerms.Any(st => EF.Functions.Like(p.Brand.Name, "%" + st + "%")) ||
                    searchTerms.Any(st => EF.Functions.Like(p.Subcategory.Name, "%" + st + "%")) ||
                    searchTerms.Any(st => EF.Functions.Like(p.Subcategory.Category.Name, "%" + st + "%")));

                // Rest of your existing code remains the same...
                // Category filtering
                if (!string.IsNullOrEmpty(query.CategoryId))
                {
                    baseQuery = baseQuery.Where(p => p.Subcategory.CategoryId == query.CategoryId);
                }

                // Brand filtering
                if (!string.IsNullOrEmpty(query.BrandId))
                {
                    baseQuery = baseQuery.Where(p => p.BrandId == query.BrandId);
                }

                if (query.MinPrice.HasValue)
                {
                    baseQuery = baseQuery.Where(p => p.Price >= query.MinPrice.Value);
                }

                if (query.MaxPrice.HasValue)
                {
                    baseQuery = baseQuery.Where(p => p.Price <= query.MaxPrice.Value);
                }

                // Use simple sorting for now - remove the complex relevance sorting
                baseQuery = query.SortBy switch
                {
                    "price_asc" => baseQuery.OrderBy(p => p.Price),
                    "price_desc" => baseQuery.OrderByDescending(p => p.Price),
                    "name_asc" => baseQuery.OrderBy(p => p.Name),
                    "name_desc" => baseQuery.OrderByDescending(p => p.Name),
                    "featured" => baseQuery.OrderByDescending(p => p.IsFeatured),
                    _ => baseQuery.OrderByDescending(p => p.IsFeatured).ThenBy(p => p.Name)
                };

                var totalCount = await baseQuery.CountAsync();
                var totalPages = (int)Math.Ceiling(totalCount / (double)query.PageSize);

                var products = await baseQuery
                    .Skip((query.Page - 1) * query.PageSize)
                    .Take(query.PageSize)
                    .Select(p => new ProductSearchResultDto
                    {
                        Id = p.Id,
                        Name = p.Name,
                        SKU = p.SKU,
                        Brand = p.Brand.Name,
                        BrandId = p.BrandId,
                        Category = $"{p.Subcategory.Category.Name} > {p.Subcategory.Name}",
                        CategoryId = p.Subcategory.CategoryId,
                        SubcategoryId = p.SubcategoryId,
                        Price = p.Price,
                        DiscountPrice = p.DiscountPrice,
                        ImageUrl = p.Images.Where(i => i.IsPrimary).Select(i => i.ImageUrl).FirstOrDefault() ?? string.Empty,
                        IsFeatured = p.IsFeatured,
                        StockStatus = p.StockQuantity > 0 ? "In Stock" : "Out of Stock",
                        StockQuantity = p.StockQuantity.ToString()
                    })
                    .ToListAsync();

                // Get available filters
                var brandFilters = await baseQuery
                    .GroupBy(p => new { p.Brand.Id, p.Brand.Name })
                    .Select(g => new BrandFilterDto
                    {
                        Id = g.Key.Id,
                        Name = g.Key.Name,
                        ProductCount = g.Count()
                    })
                    .ToListAsync();

                var categoryFilters = await baseQuery
                    .GroupBy(p => new { p.Subcategory.Category.Id, p.Subcategory.Category.Name })
                    .Select(g => new CategoryFilterDto
                    {
                        Id = g.Key.Id,
                        Name = g.Key.Name,
                        ProductCount = g.Count()
                    })
                    .ToListAsync();

                var priceRange = await baseQuery
                    .GroupBy(p => 1)
                    .Select(g => new PriceRangeDto
                    {
                        Min = g.Min(p => p.Price),
                        Max = g.Max(p => p.Price)
                    })
                    .FirstOrDefaultAsync() ?? new PriceRangeDto { Min = 0, Max = 0 };

                return Ok(new SearchResultsDto
                {
                    Query = query.Term,
                    Page = query.Page,
                    PageSize = query.PageSize,
                    TotalCount = totalCount,
                    TotalPages = totalPages,
                    Results = products,
                    Filters = new SearchFiltersDto
                    {
                        Categories = categoryFilters,
                        Brands = brandFilters,
                        PriceRange = priceRange
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching products");
                return StatusCode(500, "Internal server error");
            }
        }
        private IQueryable<Product> OrderByRelevance(IQueryable<Product> query, string[] searchTerms)
        {
            // This is a simplified relevance calculation
            // In a production system, you might want to use Full-Text Search or more sophisticated scoring
            return query
                .AsEnumerable() // Switch to client-side evaluation for complex scoring
                .AsQueryable()
                .OrderByDescending(p =>
                    searchTerms.Count(st => p.Name.ToLower().Contains(st)) * 10 + // Name matches are most important
                    searchTerms.Count(st => p.Brand.Name.ToLower().Contains(st)) * 5 + // Brand matches are important
                    searchTerms.Count(st => p.Description.ToLower().Contains(st)) * 3 + // Description matches
                    searchTerms.Count(st => p.Subcategory.Name.ToLower().Contains(st)) * 2 + // Category matches
                    (p.IsFeatured ? 5 : 0)) // Boost featured products
                .ThenBy(p => p.Name);
        }

    }

    public class SearchQueryDto
    {
        [FromQuery(Name = "term")]
        public string Term { get; set; } = string.Empty;

        [FromQuery]
        public int Page { get; set; } = 1;

        [FromQuery]
        public int PageSize { get; set; } = 20;

        [FromQuery]
        public string SortBy { get; set; } = "relevance";
        [FromQuery(Name = "categoryId")]
        public string? CategoryId { get; set; } // Make nullable

        [FromQuery(Name = "brandId")]
        public string? BrandId { get; set; }

        [FromQuery(Name = "minPrice")]
        public decimal? MinPrice { get; set; }

        [FromQuery(Name = "maxPrice")]
        public decimal? MaxPrice { get; set; }
    }

    public class SearchResultsDto
    {
        public string Query { get; set; } = string.Empty;
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }
        public List<ProductSearchResultDto> Results { get; set; } = new List<ProductSearchResultDto>();
        public SearchFiltersDto Filters { get; set; } = new SearchFiltersDto();
    }

    public class SearchFiltersDto
    {
        public List<CategoryFilterDto> Categories { get; set; } = new List<CategoryFilterDto>();
        public List<BrandFilterDto> Brands { get; set; } = new List<BrandFilterDto>();
        public PriceRangeDto PriceRange { get; set; } = new PriceRangeDto();
    }

    public class AutocompleteSuggestionDto
    {
        public string Type { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string ReferenceId { get; set; }
    }

    public class ProductSearchResultDto
    {
        public string Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string SKU { get; set; } = string.Empty;
        public string Brand { get; set; } = string.Empty;
        public string BrandId { get; set; }
        public string Category { get; set; } = string.Empty;
        public string CategoryId { get; set; }
        public string SubcategoryId { get; set; }
        public decimal Price { get; set; }
        public decimal? DiscountPrice { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
        public bool IsFeatured { get; set; }
        public string StockStatus { get; set; } = string.Empty;
        public string StockQuantity { get; set; } = string.Empty;
    }

    public class CategoryFilterDto
    {
        public string Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int ProductCount { get; set; }
    }

    public class BrandFilterDto
    {
        public string Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int ProductCount { get; set; }
    }

    public class PriceRangeDto
    {
        public decimal Min { get; set; }
        public decimal Max { get; set; }
    }
}