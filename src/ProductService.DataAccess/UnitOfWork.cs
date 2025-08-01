using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ProductService.DataAccess.Data;
using ProductService.DataAccess.Repositories;
using ProductService.Domain.Entites;
using ProductService.Domain.Interfaces;

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
        }

        public IRepository<Brand> BrandRepository { get; }
        public IRepository<Category> CategoryRepository { get; }
        public IRepository<Subcategory> SubcategoryRepository { get; }
        public IRepository<Product> ProductRepository { get; }
        public IRepository<ProductImage> ProductImageRepository { get; }
        public IRepository<ProductVariant> ProductVariantRepository { get; }

        public async Task<int> CompleteAsync() => await _context.SaveChangesAsync();

        public void Dispose() => _context.Dispose();
    }
}
