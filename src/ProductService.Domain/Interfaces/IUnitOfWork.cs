using ProductService.Domain.Entites;
using ProductService.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static ProductService.Domain.Entities.WarrantyClaim;

namespace ProductService.Domain.Interfaces;

public interface IUnitOfWork : IDisposable
{
    IRepository<Brand> BrandRepository { get; }
    IRepository<Category> CategoryRepository { get; }
    IRepository<Subcategory> SubcategoryRepository { get; }
    IRepository<Product> ProductRepository { get; }
    IRepository<ProductImage> ProductImageRepository { get; }
    IRepository<ProductVariant> ProductVariantRepository { get; }

	// New blog repositories
	public IRepository<Blog> BlogRepository { get; }
	public IRepository<BlogImage> BlogImageRepository { get; }

	// New warranty repositories
	public IRepository<WarrantyClaim> WarrantyClaimRepository { get; }
	public IRepository<WarrantyClaimImage> WarrantyClaimImageRepository { get; }

	Task<int> CompleteAsync();
}
