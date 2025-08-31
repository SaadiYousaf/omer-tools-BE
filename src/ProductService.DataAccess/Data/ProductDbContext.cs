using Microsoft.EntityFrameworkCore;
using ProductService.Domain.Entites;
using ProductService.Domain.Entities;
using System;
using UserService.Domain.Entities;

namespace ProductService.DataAccess.Data
{
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
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<Refund> Refunds { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<ShippingAddress> ShippingAddresses { get; set; }
        public DbSet<BrandCategory> BrandCategories { get; set; }
        public DbSet<Address> Addresses { get; set; }
        public DbSet<PaymentMethod> PaymentMethods { get; set; }
        public DbSet<UserPreferences> UserPreferences { get; set; }
        public DbSet<CategoryImage> CategoryImages { get; set; }
        public DbSet<SubcategoryImage> SubcategoryImages { get; set; }
        public DbSet<BrandImage> BrandImages { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Table Names
            modelBuilder.Entity<Brand>().ToTable("brands");
            modelBuilder.Entity<Category>().ToTable("categories");
            modelBuilder.Entity<Subcategory>().ToTable("subcategories");
            modelBuilder.Entity<Product>().ToTable("products");
            modelBuilder.Entity<ProductImage>().ToTable("product_images");
            modelBuilder.Entity<ProductVariant>().ToTable("product_variants");
            modelBuilder.Entity<Order>().ToTable("orders");
            modelBuilder.Entity<OrderItem>().ToTable("order_items");
            modelBuilder.Entity<Payment>().ToTable("payments");
            modelBuilder.Entity<Refund>().ToTable("refunds");
            modelBuilder.Entity<User>().ToTable("users");
            modelBuilder.Entity<ShippingAddress>().ToTable("shipping_addresses");
            modelBuilder.Entity<BrandImage>().ToTable("BrandImages");

        // Configure User entity
        modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(u => u.Id);
                entity.Property(u => u.Id).HasMaxLength(50);
                entity.Property(u => u.Email).IsRequired().HasMaxLength(255);
                entity.Property(u => u.PasswordHash).IsRequired();
                entity.Property(u => u.PasswordSalt).IsRequired(false);
                entity.Property(u => u.FirstName).HasMaxLength(100).IsRequired(false);
                entity.Property(u => u.LastName).HasMaxLength(100).IsRequired(false);
                entity.Property(u => u.PhoneNumber).HasMaxLength(20).IsRequired(false);
                entity.HasMany(u => u.Addresses)
               .WithOne(a => a.User)
               .HasForeignKey(a => a.UserId)
               .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(u => u.PaymentMethods)
                    .WithOne(p => p.User)
                    .HasForeignKey(p => p.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(u => u.Preferences)
                    .WithOne(p => p.User)
                    .HasForeignKey<UserPreferences>(p => p.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            
            // Set default values
            entity.Property(u => u.CreatedAt)
                    .HasDefaultValueSql("GETUTCDATE()");
                entity.Property(u => u.IsActive)
                    .HasDefaultValue(true);
            });

            // Configure Category entity
            modelBuilder.Entity<Brand>(entity =>
            {
                entity.HasKey(b => b.Id);
                entity.Property(b => b.Id).HasMaxLength(50);
                entity.Property(b => b.Name).IsRequired().HasMaxLength(100);
                entity.Property(b => b.Description).HasMaxLength(500);
                entity.Property(b => b.LogoUrl).HasMaxLength(255);
                entity.Property(b => b.WebsiteUrl).HasMaxLength(255);

                // Remove the old foreign key relationship to Category
                // entity.HasOne(b => b.Category)
                //     .WithMany(c => c.Brands)
                //     .HasForeignKey(b => b.CategoryId)
                //     .OnDelete(DeleteBehavior.Restrict);

                // Set up many-to-many relationship with Category through BrandCategory
                entity.HasMany(b => b.BrandCategories)
                    .WithOne(bc => bc.Brand)
                    .HasForeignKey(bc => bc.BrandId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Set default values
                entity.Property(b => b.CreatedAt)
                    .HasDefaultValueSql("GETUTCDATE()");
                entity.Property(b => b.IsActive)
                    .HasDefaultValue(true);
            });
            modelBuilder.Entity<BrandImage>(entity =>
            {
                entity.ToTable("brandimages");   // ✅ make sure DB table is named
                entity.HasKey(bi => bi.Id);
                entity.Property(bi => bi.Id).ValueGeneratedOnAdd();

                entity.HasOne(bi => bi.Brand)
                    .WithMany(b => b.Images)
                    .HasForeignKey(bi => bi.BrandId);
            });
            modelBuilder.Entity<CategoryImage>(entity =>
            {
                entity.ToTable("categoryimages");
                entity.HasKey(ci => ci.Id);
                entity.Property(ci => ci.Id).ValueGeneratedOnAdd();
                entity.HasOne(ci => ci.Category)
                    .WithMany(c => c.Images)
                    .HasForeignKey(ci => ci.CategoryId);
            });

            modelBuilder.Entity<SubcategoryImage>(entity =>
            {
                entity.ToTable("subcategoryimages");
                entity.HasKey(ci => ci.Id);
                entity.Property(ci => ci.Id).ValueGeneratedOnAdd();
                entity.HasOne(ci => ci.Subcategory)
                    .WithMany(c => c.Images)
                    .HasForeignKey(ci => ci.SubcategoryId);
            });
            // Configure the BrandCategory join entity
            modelBuilder.Entity<BrandCategory>(entity =>
            {
                entity.HasKey(bc => new { bc.BrandId, bc.CategoryId });

                entity.Property(bc => bc.BrandId).HasMaxLength(50);
                entity.Property(bc => bc.CategoryId).HasMaxLength(50);

                entity.HasOne(bc => bc.Brand)
                    .WithMany(b => b.BrandCategories)
                    .HasForeignKey(bc => bc.BrandId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(bc => bc.Category)
                    .WithMany(c => c.BrandCategories)
                    .HasForeignKey(bc => bc.CategoryId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Update Category configuration if needed
            modelBuilder.Entity<Category>(entity =>
            {
                entity.HasKey(c => c.Id);
                entity.Property(c => c.Id).HasMaxLength(50);
                entity.Property(c => c.Name).IsRequired().HasMaxLength(100);
                entity.Property(c => c.Description).HasMaxLength(500);
                entity.Property(c => c.ImageUrl).HasMaxLength(255);

                // Set up many-to-many relationship with Brand through BrandCategory
                entity.HasMany(c => c.BrandCategories)
                    .WithOne(bc => bc.Category)
                    .HasForeignKey(bc => bc.CategoryId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Set default values
                entity.Property(c => c.CreatedAt)
                    .HasDefaultValueSql("GETUTCDATE()");
                entity.Property(c => c.IsActive)
                    .HasDefaultValue(true);
            });
            // Configure Subcategory entity
            modelBuilder.Entity<Subcategory>(entity =>
            {
                entity.HasKey(s => s.Id);
                entity.Property(s => s.Id).HasMaxLength(50);
                entity.Property(s => s.Name).IsRequired().HasMaxLength(100);
                entity.Property(s => s.Description).HasMaxLength(500);
                entity.Property(s => s.ImageUrl).HasMaxLength(255);

                // Foreign key to Category
                entity.HasOne(s => s.Category)
                    .WithMany(c => c.Subcategories)
                    .HasForeignKey(s => s.CategoryId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Set default values
                entity.Property(s => s.CreatedAt)
                    .HasDefaultValueSql("GETUTCDATE()");
                entity.Property(s => s.IsActive)
                    .HasDefaultValue(true);
            });

            // Configure Product entity
            modelBuilder.Entity<Product>(entity =>
            {
                entity.HasKey(p => p.Id);
                entity.Property(p => p.Id).HasMaxLength(50);
                entity.Property(p => p.SKU).IsRequired().HasMaxLength(50);
                entity.Property(p => p.Name).IsRequired().HasMaxLength(100);
                entity.Property(p => p.Description).HasMaxLength(1000);
                entity.Property(p => p.Specifications).HasColumnType("nvarchar(max)");
                entity.Property(p => p.Price).HasColumnType("decimal(18,2)");
                entity.Property(p => p.DiscountPrice).HasColumnType("decimal(18,2)");
                entity.Property(p => p.Dimensions).HasMaxLength(50);
                entity.Property(p => p.WarrantyPeriod).HasMaxLength(50);

                // Foreign keys
                entity.HasOne(p => p.Brand)
                    .WithMany(b => b.Products)
                    .HasForeignKey(p => p.BrandId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(p => p.Subcategory)
                    .WithMany(s => s.Products)
                    .HasForeignKey(p => p.SubcategoryId)
                    .OnDelete(DeleteBehavior.Restrict);

                // Set default values
                entity.Property(p => p.CreatedAt)
                    .HasDefaultValueSql("GETUTCDATE()");
                entity.Property(p => p.IsActive)
                    .HasDefaultValue(true);
                entity.Property(p => p.IsFeatured)
                    .HasDefaultValue(false);
            });

            // Configure ProductImage entity
            modelBuilder.Entity<ProductImage>(entity =>
            {
                entity.HasKey(pi => pi.Id);
                entity.Property(pi => pi.Id).HasMaxLength(50);
                entity.Property(pi => pi.ImageUrl).IsRequired().HasMaxLength(255);
                entity.Property(pi => pi.AltText).HasMaxLength(100);

                // Foreign key to Product
                entity.HasOne(pi => pi.Product)
                    .WithMany(p => p.Images)
                    .HasForeignKey(pi => pi.ProductId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Set default values
                entity.Property(pi => pi.CreatedAt)
                    .HasDefaultValueSql("GETUTCDATE()");
                entity.Property(pi => pi.IsActive)
                    .HasDefaultValue(true);
            });

            // Configure ProductVariant entity
            modelBuilder.Entity<ProductVariant>(entity =>
            {
                entity.HasKey(pv => pv.Id);
                entity.Property(pv => pv.Id).HasMaxLength(50);
                entity.Property(pv => pv.VariantType).IsRequired().HasMaxLength(50);
                entity.Property(pv => pv.VariantValue).IsRequired().HasMaxLength(50);
                entity.Property(pv => pv.SKU).IsRequired().HasMaxLength(50);
                entity.Property(pv => pv.PriceAdjustment).HasColumnType("decimal(18,2)");

                // Foreign key to Product
                entity.HasOne(pv => pv.Product)
                    .WithMany(p => p.Variants)
                    .HasForeignKey(pv => pv.ProductId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Set default values
                entity.Property(pv => pv.CreatedAt)
                    .HasDefaultValueSql("GETUTCDATE()");
                entity.Property(pv => pv.IsActive)
                    .HasDefaultValue(true);
            });
            modelBuilder.Entity<Address>(entity =>
            {
                entity.HasKey(a => a.Id);
                entity.Property(a => a.AddressType).IsRequired().HasMaxLength(50);
                entity.Property(a => a.FullName).IsRequired().HasMaxLength(100);
                entity.Property(a => a.AddressLine1).IsRequired().HasMaxLength(200);
                entity.Property(a => a.City).IsRequired().HasMaxLength(100);
                entity.Property(a => a.State).IsRequired().HasMaxLength(100);
                entity.Property(a => a.PostalCode).IsRequired().HasMaxLength(20);
                entity.Property(a => a.Country).IsRequired().HasMaxLength(100);
            });

            // PaymentMethod configuration
            modelBuilder.Entity<PaymentMethod>(entity =>
            {
                entity.HasKey(p => p.Id);
                entity.Property(p => p.PaymentType).IsRequired().HasMaxLength(50);
                entity.Property(p => p.Provider).IsRequired().HasMaxLength(50);
                entity.Property(p => p.Last4Digits).IsRequired().HasMaxLength(4);
            });

            // UserPreferences configuration
            modelBuilder.Entity<UserPreferences>(entity =>
            {
                entity.HasKey(p => p.Id);
                entity.Property(p => p.Language).HasMaxLength(10).HasDefaultValue("en");
                entity.Property(p => p.Currency).HasMaxLength(3).HasDefaultValue("USD");
                entity.Property(p => p.Theme).HasMaxLength(10).HasDefaultValue("System");
            });
            modelBuilder.Entity<Order>(entity =>
            {
                entity.HasKey(o => o.Id);
                entity.Property(o => o.Id).HasMaxLength(50);
                entity.Property(o => o.OrderNumber).IsRequired().HasMaxLength(50);
                entity.Property(o => o.Status).IsRequired().HasMaxLength(20);
                entity.Property(o => o.TotalAmount).HasColumnType("decimal(18,2)");
                entity.Property(o => o.TaxAmount).HasColumnType("decimal(18,2)");
                entity.Property(o => o.ShippingCost).HasColumnType("decimal(18,2)");
                entity.Property(o => o.PaymentStatus).HasMaxLength(20);
                entity.Property(o => o.SessionId).HasMaxLength(100);
                entity.Property(o => o.TransactionId).HasMaxLength(100);

                // Foreign key to User
                entity.HasOne(o => o.User)
                    .WithMany(u => u.Orders)
                    .HasForeignKey(o => o.UserId)
                    .OnDelete(DeleteBehavior.Restrict);

                // Set default values
                entity.Property(o => o.CreatedAt)
                    .HasDefaultValueSql("GETUTCDATE()");
                entity.Property(o => o.IsActive)
                    .HasDefaultValue(true);
            });

            // ShippingAddress configuration
            modelBuilder.Entity<ShippingAddress>(entity =>
            {
                entity.HasKey(sa => sa.Id);
                entity.Property(sa => sa.Id).HasMaxLength(50);
                entity.Property(sa => sa.FullName).IsRequired().HasMaxLength(100);
                entity.Property(sa => sa.AddressLine1).IsRequired().HasMaxLength(255);
                entity.Property(sa => sa.AddressLine2).HasMaxLength(255);
                entity.Property(sa => sa.City).IsRequired().HasMaxLength(100);
                entity.Property(sa => sa.State).IsRequired().HasMaxLength(50);
                entity.Property(sa => sa.PostalCode).IsRequired().HasMaxLength(20);
                entity.Property(sa => sa.Country).IsRequired().HasMaxLength(50);

                // Foreign key to Order (one-to-one relationship)
                entity.HasOne(sa => sa.Order)
                    .WithOne(o => o.ShippingAddress)
                    .HasForeignKey<ShippingAddress>(sa => sa.OrderId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
            // Configure OrderItem entity
            modelBuilder.Entity<OrderItem>(entity =>
            {
                entity.HasKey(oi => oi.Id);
                entity.Property(oi => oi.Id).HasMaxLength(50);
                entity.Property(oi => oi.ProductName).IsRequired().HasMaxLength(100);
                entity.Property(oi => oi.UnitPrice).HasColumnType("decimal(18,2)");
                entity.Property(oi => oi.TotalPrice).HasColumnType("decimal(18,2)");
                entity.Property(oi => oi.ImageUrl).HasMaxLength(255);

                // Foreign key to Order
                entity.HasOne(oi => oi.Order)
                    .WithMany(o => o.Items)
                    .HasForeignKey(oi => oi.OrderId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Set default values
                entity.Property(oi => oi.CreatedAt)
                    .HasDefaultValueSql("GETUTCDATE()");
                entity.Property(oi => oi.IsActive)
                    .HasDefaultValue(true);
            });

            // Configure Payment entity
            modelBuilder.Entity<Payment>(entity =>
            {
                entity.HasKey(p => p.Id);
                entity.Property(p => p.Id).HasMaxLength(50);
                entity.Property(p => p.PaymentMethod).IsRequired().HasMaxLength(50);
                entity.Property(p => p.TransactionId).HasMaxLength(100);
                entity.Property(p => p.Amount).HasColumnType("decimal(18,2)");
                entity.Property(p => p.Status).IsRequired().HasMaxLength(20);
                entity.Property(p => p.Currency).HasMaxLength(10);
                entity.Property(p => p.Last4Digits).HasMaxLength(4);
                entity.Property(p => p.CardBrand).HasMaxLength(20);
                entity.Property(p => p.CustomerEmail).HasMaxLength(255);

                // Foreign key to Order
                entity.HasOne(p => p.Order)
                    .WithMany(o => o.Payments)
                    .HasForeignKey(p => p.OrderId)
                    .OnDelete(DeleteBehavior.Restrict);

                // Set default values
                entity.Property(p => p.CreatedAt)
                    .HasDefaultValueSql("GETUTCDATE()");
                entity.Property(p => p.IsActive)
                    .HasDefaultValue(true);
            });

            // Configure Refund entity
            modelBuilder.Entity<Refund>(entity =>
            {
                entity.HasKey(r => r.Id);
                entity.Property(r => r.Id).HasMaxLength(50);
                entity.Property(r => r.Reason).HasMaxLength(500);
                entity.Property(r => r.Amount).HasColumnType("decimal(18,2)");
                entity.Property(r => r.Status).IsRequired().HasMaxLength(20);
                entity.Property(r => r.Currency).HasMaxLength(10);

                // Foreign key to Payment
                entity.HasOne(r => r.Payment)
                    .WithMany(p => p.Refunds)
                    .HasForeignKey(r => r.PaymentId)
                    .OnDelete(DeleteBehavior.Restrict);

                // Set default values
                entity.Property(r => r.CreatedAt)
                    .HasDefaultValueSql("GETUTCDATE()");
                entity.Property(r => r.IsActive)
                    .HasDefaultValue(true);
            });
        }
    }
}