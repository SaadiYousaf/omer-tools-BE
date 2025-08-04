using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ProductService.Business.DTOs
{

    public record BrandDto(
        int Id,
        string Name,
        string Description,
        string LogoUrl,
        string WebsiteUrl,
        DateTime CreatedAt,
        DateTime? UpdatedAt,
        bool IsActive
    );

    public record CategoryDto(
        int Id,
        int BrandId,
        string Name,
        string Description,
        string ImageUrl,
        int DisplayOrder,
        DateTime CreatedAt,
        DateTime? UpdatedAt,
        bool IsActive
    );

    public record SubcategoryDto(
        int Id,
        int CategoryId,
        string Name,
        string Description,
        string ImageUrl,
        int DisplayOrder,
        DateTime CreatedAt,
        DateTime? UpdatedAt,
        bool IsActive
    );

    public record ProductDto
    {
        public int Id { get; init; }
        public int SubcategoryId { get; init; }
        public int BrandId { get; init; }
        public string SKU { get; init; } = string.Empty;
        public string Name { get; init; } = string.Empty;
        public string Description { get; init; } = string.Empty;
        public string Specifications { get; init; } = "{}";
        public decimal Price { get; init; }
        public decimal? DiscountPrice { get; init; }
        public int StockQuantity { get; init; }
        public decimal? Weight { get; init; }
        public string Dimensions { get; init; } = string.Empty;
        public bool IsFeatured { get; init; }
        public string WarrantyPeriod { get; init; } = string.Empty;
        public DateTime CreatedAt { get; init; }
        public DateTime? UpdatedAt { get; init; }
        public bool IsActive { get; init; }
        public IEnumerable<ProductImageDto> Images { get; init; } = new List<ProductImageDto>();
        public IEnumerable<ProductVariantDto> Variants { get; init; } =
            new List<ProductVariantDto>();
    }

    public record ProductImageDto(
        int Id,
        int ProductId,
        string ImageUrl,
        string AltText,
        int DisplayOrder,
        bool IsPrimary,
        DateTime CreatedAt,
        DateTime? UpdatedAt,
        bool IsActive
    )
    {
        // Add parameterless constructor for AutoMapper
        public ProductImageDto() : this(0, 0, "", "", 0, false, DateTime.UtcNow, null, true)
        {
        }
    };

    public record ProductVariantDto(
        int Id,
        int ProductId,
        string VariantType,
        string VariantValue,
        decimal PriceAdjustment,
        string SKU,
        int StockQuantity,
        DateTime CreatedAt,
        DateTime? UpdatedAt,
        bool IsActive
    );

    public record ProductFullDto(
        ProductDto Product,
        BrandDto Brand,
        SubcategoryDto Subcategory,
        CategoryDto Category,
        IEnumerable<ProductImageDto> Images,
        IEnumerable<ProductVariantDto> Variants
    );

    public record BrandWithCategoriesDto(BrandDto Brand, IEnumerable<CategoryDto> Categories);

    public record CategoryWithSubcategoriesDto(
        CategoryDto Category,
        IEnumerable<SubcategoryDto> Subcategories
    );
}
