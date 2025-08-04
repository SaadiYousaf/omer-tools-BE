using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using ProductService.Business.DTOs;
using ProductService.Domain.Entites;
using static ProductService.Business.Interfaces.IProductService;
using ProductFullDto = ProductService.Business.DTOs.ProductFullDto;

namespace ProductService.Business.Mappings
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<Brand, BrandDto>().ReverseMap();
            CreateMap<Category, CategoryDto>().ReverseMap();
            CreateMap<Subcategory, SubcategoryDto>().ReverseMap();

            CreateMap<Product, ProductDto>()
                .ForMember(dest => dest.Images, opt => opt.MapFrom(src => src.Images))
                .ForMember(dest => dest.Variants, opt => opt.MapFrom(src => src.Variants))
                .ReverseMap();
            CreateMap<ProductImage, ProductImageDto>()
       .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreatedAt))
       .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => src.UpdatedAt))
       .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => src.IsActive))
       .ReverseMap();
       
            CreateMap<ProductVariant, ProductVariantDto>().ReverseMap();

           
            CreateMap<Product, ProductFullDto>()
                .ForMember(dest => dest.Product, opt => opt.MapFrom(src => src))
                .ForMember(dest => dest.Brand, opt => opt.MapFrom(src => src.Brand))
                .ForMember(dest => dest.Subcategory, opt => opt.MapFrom(src => src.Subcategory))
                .ForMember(dest => dest.Category, opt => opt.MapFrom(src => src.Subcategory.Category));
        }
    }
}
