using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using ProductService.Business.DTOs;
using ProductService.Domain.Entites;
using ProductService.Domain.Entities;
using UserService.Domain.Entities;

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
        }
    }
}