using ProductService.Business.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProductService.Business.Interfaces
{
	public interface ISEOService
	{
		string GenerateMetaTitle(ProductFullDto product);
		string GenerateMetaDescription(ProductFullDto product);
		string GenerateMetaKeywords(ProductFullDto product);
		string GenerateCanonicalUrl(ProductFullDto product, string baseUrl);
		string GetPrimaryImageUrl(ProductFullDto product);

		string GenerateStructuredData(ProductFullDto product, string baseUrl);
		ProductDto EnhanceProductWithSEO(ProductFullDto product, string baseUrl);
	}
}
