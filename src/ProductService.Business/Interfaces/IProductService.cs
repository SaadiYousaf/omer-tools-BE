using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ProductService.Business.DTOs;

namespace ProductService.Business.Interfaces
{
    public interface IProductService
    {
        // Product methods
        Task<ProductDto> GetProductByIdAsync(string id);
        Task<IEnumerable<ProductDto>> GetAllProductsAsync();
        Task<IEnumerable<ProductDto>> GetProductsByBrandAsync(string brandId);
        Task<IEnumerable<ProductDto>> GetProductsBySubcategoryAsync(string subcategoryId);
        Task<IEnumerable<ProductDto>> GetFeaturedProductsAsync();
        Task<ProductDto> CreateProductAsync(ProductDto productDto);
        Task UpdateProductAsync(ProductDto productDto);
        Task DeleteProductAsync(string id);

        // ProductImage methods
        Task<ProductImageDto> GetProductImageByIdAsync(string id);
        Task<IEnumerable<ProductImageDto>> GetImagesByProductAsync(string productId);
        Task<ProductImageDto> CreateProductImageAsync(ProductImageDto imageDto);
        Task UpdateProductImageAsync(ProductImageDto imageDto);
        Task DeleteProductImageAsync(string id);
        Task SetPrimaryImageAsync(string productId, string imageId);

        // ProductVariant methods
        Task<ProductVariantDto> GetProductVariantByIdAsync(string id);
        Task<IEnumerable<ProductVariantDto>> GetVariantsByProductAsync(string productId);
        Task<ProductVariantDto> CreateProductVariantAsync(ProductVariantDto variantDto);
        Task UpdateProductVariantAsync(ProductVariantDto variantDto);
        Task DeleteProductVariantAsync(string id);

        // Brand methods
        Task<BrandDto> GetBrandByIdAsync(string id);
        Task<IEnumerable<BrandDto>> GetAllBrandsAsync();
        Task<IEnumerable<BrandDto>> GetBrandsByCategoryAsync(string categoryId);
        Task<BrandDto> CreateBrandAsync(BrandDto brandDto);
        Task UpdateBrandAsync(BrandDto brandDto);
        Task DeleteBrandAsync(string id);

        // Category methods
        Task<CategoryDto> GetCategoryByIdAsync(string id);
        Task<IEnumerable<CategoryDto>> GetAllCategoriesAsync();
        Task<CategoryDto> CreateCategoryAsync(CategoryDto categoryDto);
        Task UpdateCategoryAsync(CategoryDto categoryDto);
        Task DeleteCategoryAsync(string id);

        // Subcategory methods
        Task<SubcategoryDto> GetSubcategoryByIdAsync(string id);
        Task<IEnumerable<SubcategoryDto>> GetSubcategoriesByCategoryAsync(string categoryId);
        Task<IEnumerable<SubcategoryDto>> GetAllSubcategoriesAsync();
        Task<SubcategoryDto> CreateSubcategoryAsync(SubcategoryDto subcategoryDto);
        Task UpdateSubcategoryAsync(SubcategoryDto subcategoryDto);
        Task DeleteSubcategoryAsync(string id);

        // Combined methods
        Task<CategoryWithBrandsDto> GetCategoryWithBrandsAsync(string categoryId);
        Task<ProductFullDto> GetProductFullDetailsAsync(string id);
        Task<CategoryWithSubcategoriesDto> GetCategoryWithSubcategoriesAsync(string categoryId);
    }
}