using ProductService.Business.DTOs;
using ProductService.Business.Interfaces;
using System.Text;
using System.Text.Json;

namespace ProductService.Business.Services
{
	public class SEOService : ISEOService
	{
		public string GenerateMetaTitle(ProductFullDto product)
		{
			if (!string.IsNullOrEmpty(product.Product.MetaTitle))
				return product.Product.MetaTitle;

			var title = new StringBuilder();
			title.Append(product.Product.Name);

			if (product.Product.DiscountPrice.HasValue && product.Product.DiscountPrice > 0)
				title.Append($" - {product.Product.DiscountPrice.Value:C} Save {(product.Product.Price - product.Product.DiscountPrice.Value):C}");
			else
				title.Append($" - {product.Product.Price:C}");

			if (!string.IsNullOrEmpty(product.Brand.Name))
				title.Append($" | {product.Brand.Name}");

			// Limit to 60 characters for SEO best practices
			return title.Length > 60 ? title.ToString().Substring(0, 57) + "..." : title.ToString();
		}

		public string GenerateMetaDescription(ProductFullDto product)
		{
			if (!string.IsNullOrEmpty(product.Product.MetaDescription))
				return product.Product.MetaDescription;

			var description = new StringBuilder();

			if (!string.IsNullOrEmpty(product.Product.TagLine))
				description.Append(product.Product.TagLine);
			else
				description.Append(product.Product.Description.Length > 120
					? product.Product.Description.Substring(0, 117) + "..."
					: product.Product.Description);

			// Add pricing information
			if (product.Product.DiscountPrice.HasValue)
				description.Append($" Now {product.Product.DiscountPrice.Value:C}. Save {(product.Product.Price - product.Product.DiscountPrice.Value):C}!");
			else
				description.Append($" Only {product.Product.Price:C}.");

			// Add brand and category context
			description.Append($" Free shipping available. {product.Brand.Name} {product.Category.Name}.");

			// Limit to 160 characters for SEO best practices
			return description.Length > 160 ? description.ToString().Substring(0, 157) + "..." : description.ToString();
		}

		public string GenerateMetaKeywords(ProductFullDto product)
		{
			if (!string.IsNullOrEmpty(product.Product.MetaKeywords))
				return product.Product.MetaKeywords;

			var keywords = new List<string>
			{
				product.Product.Name,
				product.Brand.Name,
				product.Category.Name,
				product.Subcategory.Name,
				"buy",
				"shop",
				"online"
			};

			// Add price-related keywords
			if (product.Product.DiscountPrice.HasValue)
			{
				keywords.Add("sale");
				keywords.Add("discount");
				keywords.Add("offer");
				keywords.Add("deal");
			}

			if (product.Product.IsFeatured)
				keywords.Add("featured");

			if (product.Product.IsRedemption)
				keywords.Add("redemption");

			// Remove duplicates and join
			return string.Join(", ", keywords.Distinct());
		}

		public string GenerateCanonicalUrl(ProductFullDto product, string baseUrl)
		{
			if (!string.IsNullOrEmpty(product.Product.CanonicalUrl))
				return product.Product.CanonicalUrl;

			// Create SEO-friendly URL
			var productName = product.Product.Name
				.ToLower()
				.Replace(" ", "-")
				.Replace("/", "-")
				.Replace("\\", "-")
				.Replace("&", "and")
				.Replace("?", "")
				.Replace("!", "");

			return $"{baseUrl.TrimEnd('/')}/products/{productName}-{product.Product.SKU}";
		}

		public string GetPrimaryImageUrl(ProductFullDto product)
		{
			if (!string.IsNullOrEmpty(product.Product.OgImage))
				return product.Product.OgImage;

			var primaryImage = product.Images.FirstOrDefault(img => img.IsPrimary);
			return primaryImage?.ImageUrl ?? product.Images.FirstOrDefault()?.ImageUrl ?? string.Empty;
		}

		public ProductDto EnhanceProductWithSEO(ProductFullDto product, string baseUrl)
		{
			var enhancedProduct = product.Product;

			enhancedProduct.MetaTitle = GenerateMetaTitle(product);
			enhancedProduct.MetaDescription = GenerateMetaDescription(product);
			enhancedProduct.MetaKeywords = GenerateMetaKeywords(product);
			enhancedProduct.CanonicalUrl = GenerateCanonicalUrl(product, baseUrl);

			// Open Graph properties
			enhancedProduct.OgTitle = enhancedProduct.MetaTitle;
			enhancedProduct.OgDescription = enhancedProduct.OgDescription;
			enhancedProduct.OgImage = GetPrimaryImageUrl(product);


			return enhancedProduct;
		}

		public string GenerateStructuredData(ProductFullDto product, string baseUrl)
		{
			var structuredData = new
			{
				@context = "https://schema.org",
				@type = "Product",
				name = product.Product.Name,
				description = product.Product.MetaDescription,
				image = GetPrimaryImageUrl(product),
				sku = product.Product.SKU,
				brand = new
				{
					@type = "Brand",
					name = product.Brand.Name
				},
				offers = new
				{
					@type = "Offer",
					url = GenerateCanonicalUrl(product, baseUrl),
					priceCurrency = "USD",
					price = product.Product.DiscountPrice ?? product.Product.Price,
					priceValidUntil = DateTime.UtcNow.AddYears(1).ToString("yyyy-MM-dd"),
					availability = product.Product.StockQuantity > 0 ?
						"https://schema.org/InStock" : "https://schema.org/OutOfStock",
					itemCondition = "https://schema.org/NewCondition"
				},
				aggregateRating = new
				{
					@type = "AggregateRating",
					ratingValue = "4.5", // You can replace with actual ratings from your system
					reviewCount = "100"
				}
			};

			return JsonSerializer.Serialize(structuredData, new JsonSerializerOptions
			{
				WriteIndented = true
			});
		}
	}
}
