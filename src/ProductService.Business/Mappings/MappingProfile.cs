using AutoMapper;
using ProductService.Business.DTOs;
using ProductService.Domain.Entites;
using ProductService.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UserService.Domain.Entities;
using static ProductService.Domain.Entities.WarrantyClaim;

namespace ProductService.Business.Mappings
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // Simple mappings
            CreateMap<Brand, BrandDto>()
                .ReverseMap()
                .ForMember(dest => dest.Products, opt => opt.Ignore()); // Avoid overwriting relationships

            CreateMap<Category, CategoryDto>()
                .ReverseMap()
                .ForMember(dest => dest.Brands, opt => opt.Ignore())
                .ForMember(dest => dest.Subcategories, opt => opt.Ignore());

            CreateMap<Subcategory, SubcategoryDto>()
                .ReverseMap()
                .ForMember(dest => dest.Products, opt => opt.Ignore());

            // Product mapping with related collections
            CreateMap<Product, ProductDto>()
                .ForMember(dest => dest.Images, opt => opt.MapFrom(src => src.Images))
                .ForMember(dest => dest.TagLine, opt => opt.MapFrom(src => src.TagLine))
                .ForMember(dest => dest.Variants, opt => opt.MapFrom(src => src.Variants))
                .ReverseMap()
                .ForMember(dest => dest.Brand, opt => opt.Ignore())
                .ForMember(dest => dest.Subcategory, opt => opt.Ignore())
                .ForMember(dest => dest.IsRedemption, opt => opt.MapFrom(src => src.IsRedemption))
                .ReverseMap();           

            CreateMap<ProductImage, ProductImageDto>().ReverseMap();
            CreateMap<ProductVariant, ProductVariantDto>().ReverseMap();

            // ProductFullDto mapping - now correctly handles new relationships
            CreateMap<Product, ProductFullDto>()
                .ForMember(dest => dest.Product, opt => opt.MapFrom(src => src))
                .ForMember(dest => dest.Brand, opt => opt.MapFrom(src => src.Brand))
                .ForMember(dest => dest.Subcategory, opt => opt.MapFrom(src => src.Subcategory))
                .ForMember(dest => dest.Category, opt => opt.MapFrom(
                    src => src.Subcategory != null ? src.Subcategory.Category : null
                ))
                .ForMember(dest => dest.Images, opt => opt.MapFrom(src => src.Images))
                .ForMember(dest => dest.Variants, opt => opt.MapFrom(src => src.Variants));

            // New relationship DTOs
            CreateMap<Category, CategoryWithBrandsDto>()
                .ForMember(dest => dest.Category, opt => opt.MapFrom(src => src))
                .ForMember(dest => dest.Brands, opt => opt.MapFrom(src => src.Brands));

            CreateMap<Category, CategoryWithSubcategoriesDto>()
                .ForMember(dest => dest.Category, opt => opt.MapFrom(src => src))
                .ForMember(dest => dest.Subcategories, opt => opt.MapFrom(src => src.Subcategories));
            CreateMap<UserRegistrationDto, User>();
            CreateMap<User, UserDto>();
            CreateMap<Address, AddressDto>().ReverseMap();
            CreateMap<PaymentMethod, PaymentMethodDto>().ReverseMap();
            CreateMap<UserPreferences, UserPreferencesDto>().ReverseMap();
            CreateMap<UpdateProfileDto, User>();
            CreateMap<CreateAddressDto, Address>();
            CreateMap<UpdateAddressDto, Address>();
            CreateMap<Address, AddressDto>();

            CreateMap<CreatePaymentMethodDto, PaymentMethod>();
            CreateMap<UpdatePaymentMethodDto, PaymentMethod>();
            CreateMap<PaymentMethod, PaymentMethodDto>();

            CreateMap<UpdateUserPreferencesDto, UserPreferences>();
            CreateMap<UserPreferences, UserPreferencesDto>();

			// Blog to BlogDto and reverse
			CreateMap<Blog, BlogDto>()
				.ForMember(dest => dest.Images, opt => opt.MapFrom(src => src.Images))
				.ReverseMap()
				.ForMember(dest => dest.Images, opt => opt.Ignore()); // Handle images separately

			// Alternative: More explicit BlogDto to Blog mapping
			CreateMap<BlogDto, Blog>()
				// Map BaseEntity properties
				.ForMember(dest => dest.Id, opt => opt.MapFrom(src =>
					string.IsNullOrEmpty(src.Id) ? Guid.NewGuid().ToString() : src.Id))
				.ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreatedAt))
				.ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => src.UpdatedAt))
				.ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => src.IsActive))

				// Map Blog properties
				.ForMember(dest => dest.Title, opt => opt.MapFrom(src => src.Title))
				.ForMember(dest => dest.Slug, opt => opt.MapFrom(src => src.Slug))
				.ForMember(dest => dest.ShortDescription, opt => opt.MapFrom(src => src.ShortDescription))
				.ForMember(dest => dest.Content, opt => opt.MapFrom(src => src.Content))
				.ForMember(dest => dest.FeaturedImageUrl, opt => opt.MapFrom(src => src.FeaturedImageUrl))
				.ForMember(dest => dest.Author, opt => opt.MapFrom(src => src.Author))
				.ForMember(dest => dest.MetaTitle, opt => opt.MapFrom(src => src.MetaTitle))
				.ForMember(dest => dest.MetaDescription, opt => opt.MapFrom(src => src.MetaDescription))
				.ForMember(dest => dest.MetaKeywords, opt => opt.MapFrom(src => src.MetaKeywords))
				.ForMember(dest => dest.CanonicalUrl, opt => opt.MapFrom(src => src.CanonicalUrl))
				.ForMember(dest => dest.OgTitle, opt => opt.MapFrom(src => src.OgTitle))
				.ForMember(dest => dest.OgDescription, opt => opt.MapFrom(src => src.OgDescription))
				.ForMember(dest => dest.OgImage, opt => opt.MapFrom(src => src.OgImage))
				.ForMember(dest => dest.IsPublished, opt => opt.MapFrom(src => src.IsPublished))
				.ForMember(dest => dest.IsFeatured, opt => opt.MapFrom(src => src.IsFeatured))
				.ForMember(dest => dest.ViewCount, opt => opt.MapFrom(src => src.ViewCount))
				.ForMember(dest => dest.PublishedAt, opt => opt.MapFrom(src => src.PublishedAt))
				.ForMember(dest => dest.Images, opt => opt.Ignore()); // Don't map images from DTO

			// BlogImage mappings
			CreateMap<BlogImage, BlogImageDto>()
				.ReverseMap()
				.ForMember(dest => dest.Blog, opt => opt.Ignore()); // Don't map Blog navigation

			// BlogFullDto mapping
			CreateMap<Blog, BlogFullDto>()
				.ForMember(dest => dest.Blog, opt => opt.MapFrom(src => src))
				.ForMember(dest => dest.Images, opt => opt.MapFrom(src => src.Images));

			// ============ WARRANTY CLAIM MAPPINGS - UPDATED ============

			// WarrantyClaim to WarrantyClaimDto mapping (INCLUDE PRODUCTS)
			CreateMap<WarrantyClaim, WarrantyClaimDto>()
				.ForMember(dest => dest.FaultImages, opt => opt.MapFrom(src => src.FaultImages))
				.ForMember(dest => dest.Products, opt => opt.MapFrom(src => src.ProductClaims))
				.ForMember(dest => dest.ProofMethod, opt => opt.MapFrom(src => src.ProofMethod))
				.ForMember(dest => dest.InvoiceNumber, opt => opt.MapFrom(src => src.InvoiceNumber))
				.ReverseMap()
				.ForMember(dest => dest.FaultImages, opt => opt.Ignore())
				.ForMember(dest => dest.ProductClaims, opt => opt.Ignore());

			// Alternative: More explicit WarrantyClaimDto to WarrantyClaim mapping
			CreateMap<WarrantyClaimDto, WarrantyClaim>()
				// Map BaseEntity properties
				.ForMember(dest => dest.Id, opt => opt.MapFrom(src =>
					string.IsNullOrEmpty(src.Id) ? Guid.NewGuid().ToString() : src.Id))
				.ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreatedAt))
				.ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => src.UpdatedAt))
				.ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => src.IsActive))

				// Map WarrantyClaim properties
				.ForMember(dest => dest.ClaimNumber, opt => opt.MapFrom(src => src.ClaimNumber))
				.ForMember(dest => dest.ClaimType, opt => opt.MapFrom(src => src.ClaimType))
				.ForMember(dest => dest.ProofOfPurchasePath, opt => opt.MapFrom(src => src.ProofOfPurchasePath))
				.ForMember(dest => dest.ProofOfPurchaseFileName, opt => opt.MapFrom(src => src.ProofOfPurchaseFileName))
				.ForMember(dest => dest.FullName, opt => opt.MapFrom(src => src.FullName))
				.ForMember(dest => dest.PhoneNumber, opt => opt.MapFrom(src => src.PhoneNumber))
				.ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email))
				.ForMember(dest => dest.Address, opt => opt.MapFrom(src => src.Address))
				.ForMember(dest => dest.ModelNumber, opt => opt.MapFrom(src => src.ModelNumber))
				.ForMember(dest => dest.SerialNumber, opt => opt.MapFrom(src => src.SerialNumber))
				.ForMember(dest => dest.FaultDescription, opt => opt.MapFrom(src => src.FaultDescription))
				.ForMember(dest => dest.CommonFaultDescription, opt => opt.MapFrom(src => src.CommonFaultDescription))
				.ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status))
				.ForMember(dest => dest.StatusNotes, opt => opt.MapFrom(src => src.StatusNotes))
				.ForMember(dest => dest.AssignedTo, opt => opt.MapFrom(src => src.AssignedTo))
				.ForMember(dest => dest.SubmittedAt, opt => opt.MapFrom(src => src.SubmittedAt))
				.ForMember(dest => dest.ReviewedAt, opt => opt.MapFrom(src => src.ReviewedAt))
				.ForMember(dest => dest.CompletedAt, opt => opt.MapFrom(src => src.CompletedAt))
				.ForMember(dest => dest.FaultImages, opt => opt.Ignore())
				.ForMember(dest => dest.ProductClaims, opt => opt.Ignore()); // Add this line

			// CreateWarrantyClaimDto to WarrantyClaim mapping - UPDATED for multiple products
			CreateMap<CreateWarrantyClaimDto, WarrantyClaim>()
				// Ignore BaseEntity properties (will be set automatically)
				.ForMember(dest => dest.Id, opt => opt.Ignore())
				.ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
				.ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
				.ForMember(dest => dest.IsActive, opt => opt.Ignore())

				// Ignore auto-generated properties
				.ForMember(dest => dest.ClaimNumber, opt => opt.Ignore())
				.ForMember(dest => dest.ProofOfPurchasePath, opt => opt.Ignore())
				.ForMember(dest => dest.ProofOfPurchaseFileName, opt => opt.Ignore())

				// Map user-submitted properties
				.ForMember(dest => dest.ClaimType, opt => opt.MapFrom(src => src.ClaimType))
				.ForMember(dest => dest.FullName, opt => opt.MapFrom(src => src.FullName))
				.ForMember(dest => dest.PhoneNumber, opt => opt.MapFrom(src => src.PhoneNumber))
				.ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email))
				.ForMember(dest => dest.Address, opt => opt.MapFrom(src => src.Address))
				.ForMember(dest => dest.ProofMethod, opt => opt.MapFrom(src => src.ProofMethod))
				.ForMember(dest => dest.InvoiceNumber, opt => opt.MapFrom(src => src.InvoiceNumber))

				// For backward compatibility: map single product fields
				.ForMember(dest => dest.ModelNumber, opt => opt.MapFrom(src =>
					src.Products != null && src.Products.Any()
						? src.Products[0].ModelNumber
						: src.ModelNumber))
				.ForMember(dest => dest.SerialNumber, opt => opt.MapFrom(src =>
					src.Products != null && src.Products.Any()
						? src.Products[0].SerialNumber
						: src.SerialNumber))
				.ForMember(dest => dest.FaultDescription, opt => opt.MapFrom(src =>
					string.IsNullOrEmpty(src.CommonFaultDescription)
						? (src.Products != null && src.Products.Any()
							? src.Products[0].FaultDescription
							: src.FaultDescription)
						: src.CommonFaultDescription))

				// Map common fault description
				.ForMember(dest => dest.CommonFaultDescription, opt => opt.MapFrom(src => src.CommonFaultDescription))

				// Set default values
				.ForMember(dest => dest.Status, opt => opt.MapFrom(_ => "submitted"))
				.ForMember(dest => dest.SubmittedAt, opt => opt.MapFrom(_ => DateTime.UtcNow))
				.ForMember(dest => dest.FaultImages, opt => opt.Ignore())
				.ForMember(dest => dest.ProductClaims, opt => opt.Ignore()); // Products will be handled separately

			// ============ NEW PRODUCT CLAIM MAPPINGS ============

			// ProductClaim (Entity) to ProductClaimDto (DTO)
			CreateMap<ProductClaim, ProductClaimDto>()
				.ReverseMap()
				.ForMember(dest => dest.WarrantyClaim, opt => opt.Ignore());

			// CreateProductClaimDto to ProductClaim mapping
			CreateMap<CreateProductClaimDto, ProductClaim>()
				.ForMember(dest => dest.Id, opt => opt.Ignore())
				.ForMember(dest => dest.WarrantyClaimId, opt => opt.Ignore())
				.ForMember(dest => dest.DisplayOrder, opt => opt.Ignore())
				.ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
				.ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
				.ForMember(dest => dest.IsActive, opt => opt.Ignore())
				.ForMember(dest => dest.WarrantyClaim, opt => opt.Ignore());

			// ============ EXISTING WARRANTY CLAIM IMAGE MAPPINGS ============

			// WarrantyClaimImage mappings
			CreateMap<WarrantyClaimImage, WarrantyClaimImageDto>()
				.ReverseMap()
				.ForMember(dest => dest.WarrantyClaim, opt => opt.Ignore());

			// For creating images from uploaded files
			CreateMap<WarrantyClaimImageDto, WarrantyClaimImage>()
				.ForMember(dest => dest.Id, opt => opt.MapFrom(src =>
					string.IsNullOrEmpty(src.Id) ? Guid.NewGuid().ToString() : src.Id))
				.ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreatedAt))
				.ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => src.UpdatedAt))
				.ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => src.IsActive))
				.ForMember(dest => dest.WarrantyClaimId, opt => opt.MapFrom(src => src.WarrantyClaimId))
				.ForMember(dest => dest.ImageUrl, opt => opt.MapFrom(src => src.ImageUrl))
				.ForMember(dest => dest.FileName, opt => opt.MapFrom(src => src.FileName))
				.ForMember(dest => dest.FileType, opt => opt.MapFrom(src => src.FileType))
				.ForMember(dest => dest.FileSize, opt => opt.MapFrom(src => src.FileSize))
				.ForMember(dest => dest.DisplayOrder, opt => opt.MapFrom(src => src.DisplayOrder))
				.ForMember(dest => dest.WarrantyClaim, opt => opt.Ignore());

			CreateMap<UpdateWarrantyClaimDto, WarrantyClaim>()
	// Ignore properties that shouldn't be updated
	.ForMember(dest => dest.Id, opt => opt.Ignore())
	.ForMember(dest => dest.ClaimNumber, opt => opt.Ignore())
	.ForMember(dest => dest.Status, opt => opt.Ignore())
	.ForMember(dest => dest.ProofOfPurchasePath, opt => opt.Ignore())
	.ForMember(dest => dest.ProofOfPurchaseFileName, opt => opt.Ignore())
	.ForMember(dest => dest.SubmittedAt, opt => opt.Ignore())
	.ForMember(dest => dest.ReviewedAt, opt => opt.Ignore())
	.ForMember(dest => dest.CompletedAt, opt => opt.Ignore())
	.ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
	.ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
	.ForMember(dest => dest.IsActive, opt => opt.Ignore())
	.ForMember(dest => dest.FaultImages, opt => opt.Ignore())
	.ForMember(dest => dest.ProductClaims, opt => opt.Ignore())

	// Map only updatable fields
	.ForMember(dest => dest.FullName, opt => opt.MapFrom(src => src.FullName))
	.ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email))
	.ForMember(dest => dest.PhoneNumber, opt => opt.MapFrom(src => src.PhoneNumber))
	.ForMember(dest => dest.Address, opt => opt.MapFrom(src => src.Address))
	.ForMember(dest => dest.ClaimType, opt => opt.MapFrom(src => src.ClaimType))
	.ForMember(dest => dest.CommonFaultDescription, opt => opt.MapFrom(src => src.CommonFaultDescription))
	 .ForMember(dest => dest.ProofMethod, opt => opt.MapFrom(src => src.ProofMethod))
	.ForMember(dest => dest.InvoiceNumber, opt => opt.MapFrom(src => src.InvoiceNumber))


	// Handle legacy fields (for backward compatibility with single product claims)
	// Only map legacy fields if Products array is empty/null
	.ForMember(dest => dest.ModelNumber, opt => opt.MapFrom(src =>
		(src.Products == null || !src.Products.Any()) && !string.IsNullOrEmpty(src.LegacyModelNumber)
			? src.LegacyModelNumber
			: (src.Products != null && src.Products.Any()
				? src.Products[0].ModelNumber
				: null)))
	.ForMember(dest => dest.SerialNumber, opt => opt.MapFrom(src =>
		(src.Products == null || !src.Products.Any()) && !string.IsNullOrEmpty(src.LegacyModelNumber)
			? src.LegacySerialNumber
			: (src.Products != null && src.Products.Any()
				? src.Products[0].SerialNumber
				: null)))
	.ForMember(dest => dest.FaultDescription, opt => opt.MapFrom(src =>
		(src.Products == null || !src.Products.Any()) && !string.IsNullOrEmpty(src.LegacyModelNumber)
			? src.LegacyFaultDescription
			: (src.Products != null && src.Products.Any()
				? src.Products[0].FaultDescription
				: null)));

			// UpdateProductClaimDto to ProductClaim mapping
			CreateMap<UpdateProductClaimDto, ProductClaim>()
				.ForMember(dest => dest.Id, opt => opt.MapFrom(src =>
					string.IsNullOrEmpty(src.Id) ? Guid.NewGuid().ToString() : src.Id))
				.ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src =>
					string.IsNullOrEmpty(src.Id) ? DateTime.UtcNow : (DateTime?)null))
				.ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src =>
					string.IsNullOrEmpty(src.Id) ? DateTime.UtcNow : (DateTime?)null))
				.ForMember(dest => dest.WarrantyClaimId, opt => opt.Ignore())
				.ForMember(dest => dest.DisplayOrder, opt => opt.Ignore())
				.ForMember(dest => dest.WarrantyClaim, opt => opt.Ignore());

			// UpdateWarrantyClaimStatusDto to WarrantyClaim mapping
			CreateMap<UpdateWarrantyClaimStatusDto, WarrantyClaim>()
				.ForMember(dest => dest.Id, opt => opt.Ignore())
				.ForMember(dest => dest.ClaimNumber, opt => opt.Ignore())
				.ForMember(dest => dest.FullName, opt => opt.Ignore())
				.ForMember(dest => dest.Email, opt => opt.Ignore())
				.ForMember(dest => dest.PhoneNumber, opt => opt.Ignore())
				.ForMember(dest => dest.Address, opt => opt.Ignore())
				.ForMember(dest => dest.ClaimType, opt => opt.Ignore())
				.ForMember(dest => dest.ModelNumber, opt => opt.Ignore())
				.ForMember(dest => dest.SerialNumber, opt => opt.Ignore())
				.ForMember(dest => dest.FaultDescription, opt => opt.Ignore())
				.ForMember(dest => dest.CommonFaultDescription, opt => opt.Ignore())
				.ForMember(dest => dest.SubmittedAt, opt => opt.Ignore())
				.ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
				.ForMember(dest => dest.IsActive, opt => opt.Ignore())
				.ForMember(dest => dest.FaultImages, opt => opt.Ignore())
				.ForMember(dest => dest.ProductClaims, opt => opt.Ignore())

				// Only map status-related fields
				.ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status))
				.ForMember(dest => dest.StatusNotes, opt => opt.MapFrom(src => src.StatusNotes))
				.ForMember(dest => dest.AssignedTo, opt => opt.MapFrom(src => src.AssignedTo))
				.ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(_ => DateTime.UtcNow));
		}
    }
}