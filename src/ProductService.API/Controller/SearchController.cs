using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NpgsqlTypes;
using ProductService.DataAccess;
using ProductService.DataAccess.Data;


namespace ProductService.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SearchController : ControllerBase
    {
        private readonly ProductDbContext _context;
        private readonly ILogger<SearchController> _logger;
        private const int AutocompleteLimit = 8;
        private const int SearchResultLimit = 50;

        public SearchController(
            ProductDbContext context,
            ILogger<SearchController> logger)
        {
            _context = context;
            _logger = logger;
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
                var tsQuery = NpgsqlTsQuery.Parse(normalizedTerm);

                var suggestions = new List<AutocompleteSuggestionDto>();

                // Product matches
                var productMatches = await _context.Products
                    .Where(p => p.SearchVector.Matches(tsQuery))
                    .OrderByDescending(p => p.IsFeatured)
                    .ThenBy(p => p.Name)
                    .Take(AutocompleteLimit)
                    .Select(p => new AutocompleteSuggestionDto
                    {
                        Type = "Product",
                        Value = p.Name,
                        Category = p.Subcategory.Category.Name + " > " + p.Subcategory.Name,
                        ReferenceId = p.Id,
                        Relevance = NpgsqlFullTextExtensions.TsRank(p.SearchVector, tsQuery)
                    })
                    .OrderByDescending(x => x.Relevance)
                    .ToListAsync();

                suggestions.AddRange(productMatches);

                // Brand matches
                if (suggestions.Count < AutocompleteLimit)
                {

                    var brandMatches = await _context.Brands
    .Where(b => b.SearchVector.Matches(tsQuery) || EF.Functions.ILike(b.Name, $"%{normalizedTerm}%"))
    .Select(b => new AutocompleteSuggestionDto
    {
        ReferenceId = b.Id,
        Value = b.Name,
        Type = "Brand"
    })
    .ToListAsync();

                    suggestions.AddRange(brandMatches);
                }

                // Category matches - FIXED: Use Subcategories.Sum
                if (suggestions.Count < AutocompleteLimit)
                {
                    var categoryMatches = await _context.Categories
                        .Where(p =>
    p.SearchVector.Matches(tsQuery) ||
    EF.Functions.ILike(p.Name, $"%{normalizedTerm}%")
)
                        .OrderByDescending(c => c.Subcategories.Sum(s => s.Products.Count)) // FIXED HERE
                        .Take(AutocompleteLimit - suggestions.Count)
                        .Select(c => new AutocompleteSuggestionDto
                        {
                            Type = "Category",
                            Value = c.Name,
                            ReferenceId = c.Id,
                            Relevance = NpgsqlFullTextExtensions.TsRank(c.SearchVector, tsQuery)
                        })
                        .OrderByDescending(x => x.Relevance)
                        .ToListAsync();

                    suggestions.AddRange(categoryMatches);
                }

                return Ok(suggestions.OrderByDescending(x => x.Relevance));
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
                    return BadRequest("Search query is required");
                }

                var normalizedTerm = query.Term.Trim().ToLower();
                var tsQuery = NpgsqlTsQuery.Parse(normalizedTerm);

                var baseQuery = _context.Products
                    .Include(p => p.Brand)
                    .Include(p => p.Subcategory)
                        .ThenInclude(s => s.Category)
                    .Include(p => p.Images)
                    .AsQueryable();

                baseQuery = baseQuery.Where(p =>
         p.SearchVector.Matches(tsQuery) ||
         EF.Functions.ILike(p.Name, $"%{normalizedTerm}%") ||
         EF.Functions.ILike(p.Description, $"%{normalizedTerm}%") ||
         EF.Functions.ILike(EF.Property<string>(p, "Specifications"), $"%{normalizedTerm}%")
     );



                if (query.CategoryId.HasValue && query.CategoryId.Value > 0)
                {
                    baseQuery = baseQuery.Where(p =>
                        p.Subcategory.CategoryId == query.CategoryId ||
                        p.SubcategoryId == query.CategoryId);
                }

                if (query.BrandId.HasValue && query.BrandId.Value > 0)
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

                baseQuery = query.SortBy switch
                {
                    "price_asc" => baseQuery.OrderBy(p => p.Price),
                    "price_desc" => baseQuery.OrderByDescending(p => p.Price),
                    "name_asc" => baseQuery.OrderBy(p => p.Name),
                    "name_desc" => baseQuery.OrderByDescending(p => p.Name),
                    "featured" => baseQuery.OrderByDescending(p => p.IsFeatured),
                    _ => baseQuery.OrderByDescending(p => NpgsqlFullTextExtensions.TsRank(p.SearchVector, tsQuery))
                        .ThenByDescending(p => p.IsFeatured)
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
                        ImageUrl = p.Images
                            .Where(i => i.IsPrimary)
                            .Select(i => i.ImageUrl)
                            .FirstOrDefault(),
                        IsFeatured = p.IsFeatured,
                        StockStatus = p.StockQuantity > 0 ? "In Stock" : "Out of Stock",
                        Relevance = NpgsqlFullTextExtensions.TsRank(p.SearchVector, tsQuery)
                    })
                    .ToListAsync();

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
                        Categories = await GetCategoryFilters(tsQuery),
                        Brands = await GetBrandFilters(tsQuery),
                        PriceRange = await GetPriceRange()
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching products");
                return StatusCode(500, "Internal server error");
            }
        }

        private async Task<List<CategoryFilterDto>> GetCategoryFilters(NpgsqlTsQuery tsQuery)
        {
            return await _context.Categories
                .Include(c => c.Subcategories)
                .Where(c => c.SearchVector.Matches(tsQuery))
                .Select(c => new CategoryFilterDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    ProductCount = c.Subcategories
                        .SelectMany(s => s.Products)
                        .Count(p => p.SearchVector.Matches(tsQuery)),
                    Subcategories = c.Subcategories.Select(s => new CategoryFilterDto
                    {
                        Id = s.Id,
                        Name = s.Name,
                        ProductCount = s.Products.Count(p => p.SearchVector.Matches(tsQuery))
                    }).ToList()
                })
                .ToListAsync();
        }

        private async Task<List<BrandFilterDto>> GetBrandFilters(NpgsqlTsQuery tsQuery)
        {
            return await _context.Brands
                .Where(b => b.SearchVector.Matches(tsQuery))
                .Select(b => new BrandFilterDto
                {
                    Id = b.Id,
                    Name = b.Name,
                    ProductCount = b.Products.Count(p => p.SearchVector.Matches(tsQuery))
                })
                .ToListAsync();
        }

        private async Task<PriceRangeDto> GetPriceRange()
        {
            return new PriceRangeDto
            {
                Min = await _context.Products.MinAsync(p => p.Price),
                Max = await _context.Products.MaxAsync(p => p.Price)
            };
        }
    }

    // DTO Classes
    public class SearchQueryDto
    {
        [FromQuery(Name = "term")]
        public string Term { get; set; }

        [FromQuery]
        public int Page { get; set; } = 1;

        [FromQuery]
        public int PageSize { get; set; } = 20;

        [FromQuery]
        public string SortBy { get; set; } = "relevance";

        [FromQuery(Name = "categoryId")]
        public int? CategoryId { get; set; }

        [FromQuery(Name = "brandId")]
        public int? BrandId { get; set; }

        [FromQuery(Name = "minPrice")]
        public decimal? MinPrice { get; set; }

        [FromQuery(Name = "maxPrice")]
        public decimal? MaxPrice { get; set; }
    }

    public class SearchResultsDto
    {
        public string Query { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }
        public List<ProductSearchResultDto> Results { get; set; }
        public SearchFiltersDto Filters { get; set; }
    }

    public class SearchFiltersDto
    {
        public List<CategoryFilterDto> Categories { get; set; }
        public List<BrandFilterDto> Brands { get; set; }
        public PriceRangeDto PriceRange { get; set; }
    }

    public class AutocompleteSuggestionDto
    {
        public float Relevance { get; set; }
        public string Type { get; set; } // "Product", "Brand", or "Category"
        public string Value { get; set; }
        public string Category { get; set; } // Only for products
        public int ReferenceId { get; set; }
    }

    public class ProductSearchResultDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string SKU { get; set; }
        public string Brand { get; set; }
        public int BrandId { get; set; }
        public string Category { get; set; }
        public int CategoryId { get; set; }
        public int SubcategoryId { get; set; }
        public decimal Price { get; set; }
        public decimal? DiscountPrice { get; set; }
        public string ImageUrl { get; set; }
        public bool IsFeatured { get; set; }
        public string StockStatus { get; set; }
        public float Relevance { get; set; }
    }

    public class CategoryFilterDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int ProductCount { get; set; }
        public List<CategoryFilterDto> Subcategories { get; set; }
    }

    public class BrandFilterDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int ProductCount { get; set; }
    }

    public class PriceRangeDto
    {
        public decimal Min { get; set; }
        public decimal Max { get; set; }
    }
}