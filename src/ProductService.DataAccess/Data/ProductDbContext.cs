using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ProductService.Domain.Entites;

namespace ProductService.DataAccess.Data;

public class ProductDbContext : DbContext
{
    public ProductDbContext(DbContextOptions<ProductDbContext> options)
        : base(options) { }

    public DbSet<Brand> Brands { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<Subcategory> Subcategories { get; set; }
    public DbSet<Product> Products { get; set; }
    public DbSet<ProductImage> ProductImages { get; set; }
    public DbSet<ProductVariant> ProductVariants { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Brand>().ToTable("brands");
        modelBuilder.Entity<Category>().ToTable("categories");
        modelBuilder.Entity<Subcategory>().ToTable("subcategories");
        modelBuilder.Entity<Product>().ToTable("products");
        modelBuilder.Entity<ProductImage>().ToTable("product_images");
        modelBuilder.Entity<ProductVariant>().ToTable("product_variants");

        // Configure Brand
        modelBuilder.Entity<Brand>(entity =>
        {
            entity.HasKey(b => b.Id);
            entity.Property(b => b.Name).IsRequired().HasMaxLength(100);
            entity.Property(b => b.Description).HasMaxLength(500);
            entity.Property(b => b.LogoUrl).HasMaxLength(255);
            entity.Property(b => b.WebsiteUrl).HasMaxLength(255);
        });

        // Configure Category
        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(c => c.Id);
            entity.Property(c => c.Name).IsRequired().HasMaxLength(100);
            entity.Property(c => c.Description).HasMaxLength(500);
            entity.Property(c => c.ImageUrl).HasMaxLength(255);

            entity
                .HasOne(c => c.Brand)
                .WithMany(b => b.Categories)
                .HasForeignKey(c => c.BrandId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure Subcategory
        modelBuilder.Entity<Subcategory>(entity =>
        {
            entity.HasKey(s => s.Id);
            entity.Property(s => s.Name).IsRequired().HasMaxLength(100);
            entity.Property(s => s.Description).HasMaxLength(500);
            entity.Property(s => s.ImageUrl).HasMaxLength(255);

            entity
                .HasOne(s => s.Category)
                .WithMany(c => c.Subcategories)
                .HasForeignKey(s => s.CategoryId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure Product
        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.Property(p => p.SKU).IsRequired().HasMaxLength(50);
            entity.Property(p => p.Name).IsRequired().HasMaxLength(100);
            entity.Property(p => p.Description).HasMaxLength(1000);
            entity.Property(p => p.Specifications).HasColumnType("jsonb");
            entity.Property(p => p.Price).HasColumnType("decimal(18,2)");
            entity.Property(p => p.DiscountPrice).HasColumnType("decimal(18,2)");
            entity.Property(p => p.Dimensions).HasMaxLength(50);
            entity.Property(p => p.WarrantyPeriod).HasMaxLength(50);

            entity
                .HasOne(p => p.Brand)
                .WithMany(b => b.Products)
                .HasForeignKey(p => p.BrandId)
                .OnDelete(DeleteBehavior.Restrict);

            entity
                .HasOne(p => p.Subcategory)
                .WithMany(s => s.Products)
                .HasForeignKey(p => p.SubcategoryId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Configure ProductImage
        modelBuilder.Entity<ProductImage>(entity =>
        {
            entity.HasKey(pi => pi.Id);
            entity.Property(pi => pi.ImageUrl).IsRequired().HasMaxLength(255);
            entity.Property(pi => pi.AltText).HasMaxLength(100);

            entity
                .HasOne(pi => pi.Product)
                .WithMany(p => p.Images)
                .HasForeignKey(pi => pi.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure ProductVariant
        modelBuilder.Entity<ProductVariant>(entity =>
        {
            entity.HasKey(pv => pv.Id);
            entity.Property(pv => pv.VariantType).IsRequired().HasMaxLength(50);
            entity.Property(pv => pv.VariantValue).IsRequired().HasMaxLength(50);
            entity.Property(pv => pv.SKU).IsRequired().HasMaxLength(50);
            entity.Property(pv => pv.PriceAdjustment).HasColumnType("decimal(18,2)");

            entity
                .HasOne(pv => pv.Product)
                .WithMany(p => p.Variants)
                .HasForeignKey(pv => pv.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure BaseEntity properties (if using base class)
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            modelBuilder
                .Entity(entityType.ClrType)
                .Property<DateTime>("CreatedAt")
                .HasDefaultValueSql("NOW()");

            modelBuilder.Entity(entityType.ClrType).Property<DateTime?>("UpdatedAt");

            modelBuilder
                .Entity(entityType.ClrType)
                .Property<bool>("IsActive")
                .HasDefaultValue(true);
        }
    }
}
