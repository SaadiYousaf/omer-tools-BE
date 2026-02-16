using ProductService.DataAccess.Data;
using ProductService.DataAccess.Repositories;
using ProductService.Domain.Entites;
using ProductService.Domain.Entities;
using ProductService.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static ProductService.Domain.Entities.WarrantyClaim;

namespace src.ProductService.DataAccess
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly ProductDbContext _context;

        public UnitOfWork(ProductDbContext context)
        {
            _context = context;
            BrandRepository = new Repository<Brand>(_context);
            CategoryRepository = new Repository<Category>(_context);
            SubcategoryRepository = new Repository<Subcategory>(_context);
            ProductRepository = new Repository<Product>(_context);
            ProductImageRepository = new Repository<ProductImage>(_context);
            ProductVariantRepository = new Repository<ProductVariant>(_context);
            UserRepository = new UserRepository(_context);
            CategoryImageRepository = new Repository<CategoryImage>(_context);
            SubcategoryImageRepository = new Repository<SubcategoryImage>(_context);
            BrandImageRepository = new Repository<BrandImage>(_context);

			// New blog repositories
			BlogRepository = new Repository<Blog>(_context);
			BlogImageRepository = new Repository<BlogImage>(_context);
			// New warranty claim repositories
			WarrantyClaimRepository = new Repository<WarrantyClaim>(_context);
			WarrantyClaimImageRepository = new Repository<WarrantyClaimImage>(_context);
		}
        public IRepository<SubcategoryImage> SubcategoryImageRepository { get; }
        public IRepository<BrandImage> BrandImageRepository { get; }

        public IRepository<CategoryImage> CategoryImageRepository { get; }
        public IRepository<Brand> BrandRepository { get; }
        public IRepository<Category> CategoryRepository { get; }
        public IRepository<Subcategory> SubcategoryRepository { get; }
        public IRepository<Product> ProductRepository { get; }
        public IRepository<ProductImage> ProductImageRepository { get; }
        public IRepository<ProductVariant> ProductVariantRepository { get; }
        public IUserRepository UserRepository { get; }

		// New blog repositories
		public IRepository<Blog> BlogRepository { get; }
		public IRepository<BlogImage> BlogImageRepository { get; }

		// New warranty claim repositories
		public IRepository<WarrantyClaim> WarrantyClaimRepository { get; }
		public IRepository<WarrantyClaimImage> WarrantyClaimImageRepository { get; }

		public async Task<int> CompleteAsync() => await _context.SaveChangesAsync();

        public void Dispose() => _context.Dispose();
    }
}
