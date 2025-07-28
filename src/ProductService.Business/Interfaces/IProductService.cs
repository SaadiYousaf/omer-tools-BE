using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ProductService.Business.DTOs;

namespace ProductService.Business.Interfaces
{
   
        public interface IProductService
        {
            // Product methods
            Task<ProductDto> GetProductByIdAsync(int id);
            Task<IEnumerable<ProductDto>> GetAllProductsAsync();
            Task<IEnumerable<ProductDto>> GetProductsByBrandAsync(int brandId);
            Task<IEnumerable<ProductDto>> GetProductsBySubcategoryAsync(int subcategoryId);
            Task<IEnumerable<ProductDto>> GetFeaturedProductsAsync();
            Task<ProductDto> CreateProductAsync(ProductDto productDto);
            Task UpdateProductAsync(ProductDto productDto);
            Task DeleteProductAsync(int id);

            // ProductImage methods
            Task<ProductImageDto> GetProductImageByIdAsync(int id);
            Task<IEnumerable<ProductImageDto>> GetImagesByProductAsync(int productId);
            Task<ProductImageDto> CreateProductImageAsync(ProductImageDto imageDto);
            Task UpdateProductImageAsync(ProductImageDto imageDto);
            Task DeleteProductImageAsync(int id);
            Task SetPrimaryImageAsync(int productId, int imageId);

            // ProductVariant methods
            Task<ProductVariantDto> GetProductVariantByIdAsync(int id);
            Task<IEnumerable<ProductVariantDto>> GetVariantsByProductAsync(int productId);
            Task<ProductVariantDto> CreateProductVariantAsync(ProductVariantDto variantDto);
            Task UpdateProductVariantAsync(ProductVariantDto variantDto);
            Task DeleteProductVariantAsync(int id);

            // Brand methods
            Task<BrandDto> GetBrandByIdAsync(int id);
            Task<IEnumerable<BrandDto>> GetAllBrandsAsync();
            Task<BrandDto> CreateBrandAsync(BrandDto brandDto);
            Task UpdateBrandAsync(BrandDto brandDto);
            Task DeleteBrandAsync(int id);

            // Category methods
            Task<CategoryDto> GetCategoryByIdAsync(int id);
            Task<IEnumerable<CategoryDto>> GetCategoriesByBrandAsync(int brandId);
            Task<IEnumerable<CategoryDto>> GetAllCategoriesAsync();
            Task<CategoryDto> CreateCategoryAsync(CategoryDto categoryDto);
            Task UpdateCategoryAsync(CategoryDto categoryDto);
            Task DeleteCategoryAsync(int id);

            // Subcategory methods
            Task<SubcategoryDto> GetSubcategoryByIdAsync(int id);
            Task<IEnumerable<SubcategoryDto>> GetSubcategoriesByCategoryAsync(int categoryId);
            Task<IEnumerable<SubcategoryDto>> GetAllSubcategoriesAsync();
            Task<SubcategoryDto> CreateSubcategoryAsync(SubcategoryDto subcategoryDto);
            Task UpdateSubcategoryAsync(SubcategoryDto subcategoryDto);
            Task DeleteSubcategoryAsync(int id);

            // Combined methods
            Task<ProductFullDto> GetProductFullDetailsAsync(int id);
            Task<BrandWithCategoriesDto> GetBrandWithCategoriesAsync(int brandId);
            Task<CategoryWithSubcategoriesDto> GetCategoryWithSubcategoriesAsync(int categoryId);

        public record ProductFullDto(
               ProductDto Product,
               BrandDto Brand,
               SubcategoryDto Subcategory,
               CategoryDto Category,
               IEnumerable<ProductImageDto> Images,
               IEnumerable<ProductVariantDto> Variants);

        public record BrandWithCategoriesDto(
            BrandDto Brand,
            IEnumerable<CategoryDto> Categories);

        public record CategoryWithSubcategoriesDto(
            CategoryDto Category,
            IEnumerable<SubcategoryDto> Subcategories);
    }

        // Additional DTOs for combined operations
       
    }

