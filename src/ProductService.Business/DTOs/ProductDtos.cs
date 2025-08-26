using System;
using System.Collections.Generic;

namespace ProductService.Business.DTOs
{
    public record BrandDto(
        string Id,
        List<string> CategoryIds,  // Changed from string CategoryId to List<string> CategoryIds
        string Name,
        string Description,
        string LogoUrl,
        string WebsiteUrl,
        DateTime CreatedAt,
        DateTime? UpdatedAt,
        bool IsActive
    )
    {
        public BrandDto() : this("0", new List<string>(), "", "", "", "", DateTime.UtcNow, null, true) { }
    }

    public record CategoryDto(
        string Id,
        string Name,
        string Description,
        string ImageUrl,
        int DisplayOrder,
        DateTime CreatedAt,
        DateTime? UpdatedAt,
        bool IsActive
    )
    {
        public CategoryDto() : this("0", "", "", "", 0, DateTime.UtcNow, null, true) { }
    }

    public record SubcategoryDto(
        string Id,
        string CategoryId,
        string Name,
        string Description,
        string ImageUrl,
        int DisplayOrder,
        DateTime CreatedAt,
        DateTime? UpdatedAt,
        bool IsActive
    )
    {
        public SubcategoryDto() : this("0", "0", "", "", "", 0, DateTime.UtcNow, null, true) { }
    }

    public class ProductDto
    {
        public string Id { get; set; } = string.Empty;
        public string SubcategoryId { get; set; } = string.Empty;
        public string BrandId { get; set; } = string.Empty;
        public string SKU { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Specifications { get; set; } = "{}";
        public decimal Price { get; set; }
        public string TagLine { get; set; } = string.Empty;
        public bool IsRedemption { get; set; } 
        public decimal? DiscountPrice { get; set; }
        public int StockQuantity { get; set; }
        public decimal? Weight { get; set; }
        public string Dimensions { get; set; } = string.Empty;
        public bool IsFeatured { get; set; }
        public string WarrantyPeriod { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public bool IsActive { get; set; } = true;
        public IEnumerable<ProductImageDto> Images { get; set; } = new List<ProductImageDto>();
        public IEnumerable<ProductVariantDto> Variants { get; set; } = new List<ProductVariantDto>();
    }
    public record ProductImageDto(
        string Id,
        string ProductId,
        string ImageUrl,
        string AltText,
        int DisplayOrder,
        bool IsPrimary,
        DateTime CreatedAt,
        DateTime? UpdatedAt,
        bool IsActive
    )
    {
        public ProductImageDto() : this("0", "0", "", "", 0, false, DateTime.UtcNow, null, true) { }
    }

    public record ProductVariantDto(
        string Id,
        string ProductId,
        string VariantType,
        string VariantValue,
        decimal PriceAdjustment,
        string SKU,
        int StockQuantity,
        DateTime CreatedAt,
        DateTime? UpdatedAt,
        bool IsActive
    )
    {
        public ProductVariantDto() : this("0", "0", "", "", 0, "", 0, DateTime.UtcNow, null, true) { }
    }

    public record ProductFullDto(
        ProductDto Product,
        BrandDto Brand,
        SubcategoryDto Subcategory,
        CategoryDto Category,
        IEnumerable<ProductImageDto> Images,
        IEnumerable<ProductVariantDto> Variants
    )
    {
        public ProductFullDto() : this(new ProductDto(), new BrandDto(), new SubcategoryDto(), new CategoryDto(), new List<ProductImageDto>(), new List<ProductVariantDto>()) { }
    }

    public record CategoryWithBrandsDto(
        CategoryDto Category,
        IEnumerable<BrandDto> Brands
    )
    {
        public CategoryWithBrandsDto() : this(new CategoryDto(), new List<BrandDto>()) { }
    }

    public record CategoryWithSubcategoriesDto(
        CategoryDto Category,
        IEnumerable<SubcategoryDto> Subcategories
    )
    {
        public CategoryWithSubcategoriesDto() : this(new CategoryDto(), new List<SubcategoryDto>()) { }
    }

    public record CategoryFullDto(
        CategoryDto Category,
        IEnumerable<BrandDto> Brands,
        IEnumerable<SubcategoryDto> Subcategories
    )
    {
        public CategoryFullDto() : this(new CategoryDto(), new List<BrandDto>(), new List<SubcategoryDto>()) { }
    }
}