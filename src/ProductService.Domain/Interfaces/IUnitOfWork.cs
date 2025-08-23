using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ProductService.Domain.Entites;
using ProductService.Domain.Entities;

namespace ProductService.Domain.Interfaces;

public interface IUnitOfWork : IDisposable
{
    IRepository<Brand> BrandRepository { get; }
    IRepository<Category> CategoryRepository { get; }
    IRepository<Subcategory> SubcategoryRepository { get; }
    IRepository<Product> ProductRepository { get; }
    IRepository<ProductImage> ProductImageRepository { get; }
    IRepository<ProductVariant> ProductVariantRepository { get; }

    Task<int> CompleteAsync();
}
