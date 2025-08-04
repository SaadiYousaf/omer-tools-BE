using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using ProductService.Business.DTOs;
using ProductService.Business.Interfaces;
using ProductService.Domain.Entites;
using ProductService.Domain.Interfaces;

namespace ProductService.Business.Services
{
    public class ProductService : IProductService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public ProductService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        #region Product Methods

        public async Task<ProductDto> GetProductByIdAsync(int id)
        {
            var product = await _unitOfWork.ProductRepository.GetByIdAsync(id, "Images");
            return _mapper.Map<ProductDto>(product);
        }


        public async Task<IEnumerable<ProductDto>> GetAllProductsAsync()
        {
            var products = await _unitOfWork.ProductRepository.GetAllAsync("Images");
            return _mapper.Map<IEnumerable<ProductDto>>(products);
        }

        public async Task<IEnumerable<ProductDto>> GetProductsByBrandAsync(int brandId)
        {
            var products = await _unitOfWork.ProductRepository.GetAllAsync("Images");
            return _mapper.Map<IEnumerable<ProductDto>>(
                products.Where(p => p.BrandId == brandId)
            );
        }



        public async Task<IEnumerable<ProductDto>> GetProductsBySubcategoryAsync(int subcategoryId)
        {
            var products = await _unitOfWork.ProductRepository.GetAllAsync("Images");
            return _mapper.Map<IEnumerable<ProductDto>>(
                products.Where(p => p.SubcategoryId == subcategoryId)
            );
        }
        public async Task<IEnumerable<ProductDto>> GetFeaturedProductsAsync()
        {
            var products = await _unitOfWork.ProductRepository.GetAllAsync("Images");
            return _mapper.Map<IEnumerable<ProductDto>>(products.Where(p => p.IsFeatured));
        }

        public async Task<ProductDto> CreateProductAsync(ProductDto productDto)
        {
            var product = _mapper.Map<Product>(productDto);
            await _unitOfWork.ProductRepository.AddAsync(product);
            await _unitOfWork.CompleteAsync();
            return _mapper.Map<ProductDto>(product);
        }

        public async Task UpdateProductAsync(ProductDto productDto)
        {
            var existingProduct = await _unitOfWork.ProductRepository.GetByIdAsync(productDto.Id);
            if (existingProduct == null)
                throw new KeyNotFoundException($"Product with ID {productDto.Id} not found");

            _mapper.Map(productDto, existingProduct);
            await _unitOfWork.ProductRepository.UpdateAsync(existingProduct);
            await _unitOfWork.CompleteAsync();
        }

        public async Task DeleteProductAsync(int id)
        {
            var product = await _unitOfWork.ProductRepository.GetByIdAsync(id);
            if (product == null)
                throw new KeyNotFoundException($"Product with ID {id} not found");

            await _unitOfWork.ProductRepository.DeleteAsync(product);
            await _unitOfWork.CompleteAsync();
        }
        #endregion

        #region Brand Methods
        public async Task<BrandDto> GetBrandByIdAsync(int id)
        {
            var brand = await _unitOfWork.BrandRepository.GetByIdAsync(id);
            return _mapper.Map<BrandDto>(brand);
        }

        public async Task<IEnumerable<BrandDto>> GetAllBrandsAsync()
        {
            var brands = await _unitOfWork.BrandRepository.GetAllAsync();
            return _mapper.Map<IEnumerable<BrandDto>>(brands);
        }

        public async Task<BrandDto> CreateBrandAsync(BrandDto brandDto)
        {
            var brand = _mapper.Map<Brand>(brandDto);
            await _unitOfWork.BrandRepository.AddAsync(brand);
            await _unitOfWork.CompleteAsync();
            return _mapper.Map<BrandDto>(brand);
        }

        public async Task UpdateBrandAsync(BrandDto brandDto)
        {
            var existingBrand = await _unitOfWork.BrandRepository.GetByIdAsync(brandDto.Id);
            if (existingBrand == null)
                throw new KeyNotFoundException($"Brand with ID {brandDto.Id} not found");

            _mapper.Map(brandDto, existingBrand);
            await _unitOfWork.BrandRepository.UpdateAsync(existingBrand);
            await _unitOfWork.CompleteAsync();
        }

        public async Task DeleteBrandAsync(int id)
        {
            var brand = await _unitOfWork.BrandRepository.GetByIdAsync(id);
            if (brand == null)
                throw new KeyNotFoundException($"Brand with ID {id} not found");

            await _unitOfWork.BrandRepository.DeleteAsync(brand);
            await _unitOfWork.CompleteAsync();
        }
        #endregion

        #region Category Methods
        public async Task<CategoryDto> GetCategoryByIdAsync(int id)
        {
            var category = await _unitOfWork.CategoryRepository.GetByIdAsync(id);
            return _mapper.Map<CategoryDto>(category);
        }

        public async Task<IEnumerable<CategoryDto>> GetCategoriesByBrandAsync(int brandId)
        {
            var categories = await _unitOfWork.CategoryRepository.GetAllAsync();
            return _mapper.Map<IEnumerable<CategoryDto>>(
                categories.Where(c => c.BrandId == brandId)
            );
        }

        public async Task<IEnumerable<CategoryDto>> GetAllCategoriesAsync()
        {
            var categories = await _unitOfWork.CategoryRepository.GetAllAsync();
            return _mapper.Map<IEnumerable<CategoryDto>>(categories);
        }

        public async Task<CategoryDto> CreateCategoryAsync(CategoryDto categoryDto)
        {
            var category = _mapper.Map<Category>(categoryDto);
            await _unitOfWork.CategoryRepository.AddAsync(category);
            await _unitOfWork.CompleteAsync();
            return _mapper.Map<CategoryDto>(category);
        }

        public async Task UpdateCategoryAsync(CategoryDto categoryDto)
        {
            var existingCategory = await _unitOfWork.CategoryRepository.GetByIdAsync(
                categoryDto.Id
            );
            if (existingCategory == null)
                throw new KeyNotFoundException($"Category with ID {categoryDto.Id} not found");

            _mapper.Map(categoryDto, existingCategory);
            await _unitOfWork.CategoryRepository.UpdateAsync(existingCategory);
            await _unitOfWork.CompleteAsync();
        }

        public async Task DeleteCategoryAsync(int id)
        {
            var category = await _unitOfWork.CategoryRepository.GetByIdAsync(id);
            if (category == null)
                throw new KeyNotFoundException($"Category with ID {id} not found");

            await _unitOfWork.CategoryRepository.DeleteAsync(category);
            await _unitOfWork.CompleteAsync();
        }
        #endregion

        #region Subcategory Methods
        public async Task<SubcategoryDto> GetSubcategoryByIdAsync(int id)
        {
            var subcategory = await _unitOfWork.SubcategoryRepository.GetByIdAsync(id);
            return _mapper.Map<SubcategoryDto>(subcategory);
        }

        public async Task<IEnumerable<SubcategoryDto>> GetSubcategoriesByCategoryAsync(
            int categoryId
        )
        {
            var subcategories = await _unitOfWork.SubcategoryRepository.GetAllAsync();
            return _mapper.Map<IEnumerable<SubcategoryDto>>(
                subcategories.Where(s => s.CategoryId == categoryId)
            );
        }

        public async Task<IEnumerable<SubcategoryDto>> GetAllSubcategoriesAsync()
        {
            var subcategories = await _unitOfWork.SubcategoryRepository.GetAllAsync();
            return _mapper.Map<IEnumerable<SubcategoryDto>>(subcategories);
        }

        public async Task<SubcategoryDto> CreateSubcategoryAsync(SubcategoryDto subcategoryDto)
        {
            var subcategory = _mapper.Map<Subcategory>(subcategoryDto);
            await _unitOfWork.SubcategoryRepository.AddAsync(subcategory);
            await _unitOfWork.CompleteAsync();
            return _mapper.Map<SubcategoryDto>(subcategory);
        }

        public async Task UpdateSubcategoryAsync(SubcategoryDto subcategoryDto)
        {
            var existingSubcategory = await _unitOfWork.SubcategoryRepository.GetByIdAsync(
                subcategoryDto.Id
            );
            if (existingSubcategory == null)
                throw new KeyNotFoundException(
                    $"Subcategory with ID {subcategoryDto.Id} not found"
                );

            _mapper.Map(subcategoryDto, existingSubcategory);
            await _unitOfWork.SubcategoryRepository.UpdateAsync(existingSubcategory);
            await _unitOfWork.CompleteAsync();
        }

        public async Task DeleteSubcategoryAsync(int id)
        {
            var subcategory = await _unitOfWork.SubcategoryRepository.GetByIdAsync(id);
            if (subcategory == null)
                throw new KeyNotFoundException($"Subcategory with ID {id} not found");

            await _unitOfWork.SubcategoryRepository.DeleteAsync(subcategory);
            await _unitOfWork.CompleteAsync();
        }
        #endregion

        #region ProductImage Methods
        public async Task<ProductImageDto> GetProductImageByIdAsync(int id)
        {
            var image = await _unitOfWork.ProductImageRepository.GetByIdAsync(id);
            return _mapper.Map<ProductImageDto>(image);
        }

        public async Task<IEnumerable<ProductImageDto>> GetImagesByProductAsync(int productId)
        {
            var images = await _unitOfWork.ProductImageRepository.GetAllAsync();
            return _mapper.Map<IEnumerable<ProductImageDto>>(
                images.Where(i => i.ProductId == productId)
            );
        }

        public async Task<ProductImageDto> CreateProductImageAsync(ProductImageDto imageDto)
        {
            var image = new ProductImage
            {
                ProductId = imageDto.ProductId,
                ImageUrl = imageDto.ImageUrl,
                AltText = imageDto.AltText,
                DisplayOrder = imageDto.DisplayOrder,
                IsPrimary = imageDto.IsPrimary,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            await _unitOfWork.ProductImageRepository.AddAsync(image);
            await _unitOfWork.CompleteAsync();

            return new ProductImageDto(
                image.Id,
                image.ProductId,
                image.ImageUrl,
                image.AltText,
                image.DisplayOrder,
                image.IsPrimary,
                image.CreatedAt,
                image.UpdatedAt,
                image.IsActive
            );
        }

        public async Task UpdateProductImageAsync(ProductImageDto imageDto)
        {
            var existingImage = await _unitOfWork.ProductImageRepository.GetByIdAsync(imageDto.Id);
            if (existingImage == null)
                throw new KeyNotFoundException($"Product image with ID {imageDto.Id} not found");

            _mapper.Map(imageDto, existingImage);
            await _unitOfWork.ProductImageRepository.UpdateAsync(existingImage);
            await _unitOfWork.CompleteAsync();
        }

        public async Task DeleteProductImageAsync(int id)
        {
            var image = await _unitOfWork.ProductImageRepository.GetByIdAsync(id);
            if (image == null)
                throw new KeyNotFoundException($"Product image with ID {id} not found");

            await _unitOfWork.ProductImageRepository.DeleteAsync(image);
            await _unitOfWork.CompleteAsync();
        }

        public async Task SetPrimaryImageAsync(int productId, int imageId)
        {
            var images = await _unitOfWork.ProductImageRepository.GetAllAsync();
            var productImages = images.Where(i => i.ProductId == productId).ToList();

            foreach (var img in productImages)
            {
                img.IsPrimary = (img.Id == imageId);
                await _unitOfWork.ProductImageRepository.UpdateAsync(img);
            }

            await _unitOfWork.CompleteAsync();
        }
        #endregion

        #region ProductVariant Methods
        public async Task<ProductVariantDto> GetProductVariantByIdAsync(int id)
        {
            var variant = await _unitOfWork.ProductVariantRepository.GetByIdAsync(id);
            return _mapper.Map<ProductVariantDto>(variant);
        }

        public async Task<IEnumerable<ProductVariantDto>> GetVariantsByProductAsync(int productId)
        {
            var variants = await _unitOfWork.ProductVariantRepository.GetAllAsync();
            return _mapper.Map<IEnumerable<ProductVariantDto>>(
                variants.Where(v => v.ProductId == productId)
            );
        }

        public async Task<ProductVariantDto> CreateProductVariantAsync(ProductVariantDto variantDto)
        {
            var variant = _mapper.Map<ProductVariant>(variantDto);
            await _unitOfWork.ProductVariantRepository.AddAsync(variant);
            await _unitOfWork.CompleteAsync();
            return _mapper.Map<ProductVariantDto>(variant);
        }

        public async Task UpdateProductVariantAsync(ProductVariantDto variantDto)
        {
            var existingVariant = await _unitOfWork.ProductVariantRepository.GetByIdAsync(
                variantDto.Id
            );
            if (existingVariant == null)
                throw new KeyNotFoundException(
                    $"Product variant with ID {variantDto.Id} not found"
                );

            _mapper.Map(variantDto, existingVariant);
            await _unitOfWork.ProductVariantRepository.UpdateAsync(existingVariant);
            await _unitOfWork.CompleteAsync();
        }

        public async Task DeleteProductVariantAsync(int id)
        {
            var variant = await _unitOfWork.ProductVariantRepository.GetByIdAsync(id);
            if (variant == null)
                throw new KeyNotFoundException($"Product variant with ID {id} not found");

            await _unitOfWork.ProductVariantRepository.DeleteAsync(variant);
            await _unitOfWork.CompleteAsync();
        }
        #endregion

        #region Combined Methods
        public async Task<ProductFullDto> GetProductFullDetailsAsync(int id)
        {
            var product = await _unitOfWork.ProductRepository.GetByIdAsync(id, "Images");
            if (product == null) return null;

            var brand = await _unitOfWork.BrandRepository.GetByIdAsync(product.BrandId);
            var subcategory = await _unitOfWork.SubcategoryRepository.GetByIdAsync(product.SubcategoryId);
            var category = await _unitOfWork.CategoryRepository.GetByIdAsync(subcategory.CategoryId);

            return new ProductFullDto(
                _mapper.Map<ProductDto>(product),
                _mapper.Map<BrandDto>(brand),
                _mapper.Map<SubcategoryDto>(subcategory),
                _mapper.Map<CategoryDto>(category),
                _mapper.Map<IEnumerable<ProductImageDto>>(product.Images),
                _mapper.Map<IEnumerable<ProductVariantDto>>(product.Variants)
            );
        }

        public async Task<BrandWithCategoriesDto> GetBrandWithCategoriesAsync(int brandId)
        {
            var brand = await _unitOfWork.BrandRepository.GetByIdAsync(brandId);
            if (brand == null)
                return null;

            var categories = await GetCategoriesByBrandAsync(brandId);
            return new BrandWithCategoriesDto(_mapper.Map<BrandDto>(brand), categories);
        }

        public async Task<CategoryWithSubcategoriesDto> GetCategoryWithSubcategoriesAsync(
            int categoryId
        )
        {
            var category = await _unitOfWork.CategoryRepository.GetByIdAsync(categoryId);
            if (category == null)
                return null;

            var subcategories = await GetSubcategoriesByCategoryAsync(categoryId);
            return new CategoryWithSubcategoriesDto(
                _mapper.Map<CategoryDto>(category),
                subcategories
            );
        }

        Task<IProductService.ProductFullDto> IProductService.GetProductFullDetailsAsync(int id)
        {
            throw new NotImplementedException();
        }

        Task<IProductService.BrandWithCategoriesDto> IProductService.GetBrandWithCategoriesAsync(
            int brandId
        )
        {
            throw new NotImplementedException();
        }

        Task<IProductService.CategoryWithSubcategoriesDto> IProductService.GetCategoryWithSubcategoriesAsync(
            int categoryId
        )
        {
            throw new NotImplementedException();
        }
        #endregion
    }
}