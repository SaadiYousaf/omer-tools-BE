using Microsoft.AspNetCore.Http;
using ProductService.Business.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ProductService.Business.Interfaces
{
	public interface IWarrantyService
	{
		// Warranty Claim Methods
		Task<WarrantyClaimDto> GetWarrantyClaimByIdAsync(string id);
		Task<WarrantyPaginatedResult<WarrantyClaimDto>> GetWarrantyClaimsAsync(WarrantyClaimQueryParameters queryParams);
		Task<WarrantyClaimDto> CreateWarrantyClaimAsync(CreateWarrantyClaimDto claimDto);

		Task<WarrantyClaimDto> UpdateWarrantyClaimAsync(string id, UpdateWarrantyClaimDto claimDto);
		Task UpdateWarrantyClaimStatusAsync(string id, UpdateWarrantyClaimStatusDto statusDto);
		Task DeleteWarrantyClaimAsync(string id);

		Task<string> UploadProofOfPurchaseAsync(string claimId, IFormFile file);
		Task<IEnumerable<WarrantyClaimImageDto>> UploadFaultImagesAsync(string claimId, List<IFormFile> files);
		Task UpdateProofOfPurchaseAsync(string claimId, string filePath, string fileName);

		// Dashboard & Reports
		Task<WarrantyClaimDashboardDto> GetDashboardStatsAsync();

		// Image Methods
		Task<WarrantyClaimImageDto> GetWarrantyClaimImageByIdAsync(string id);
		Task<IEnumerable<WarrantyClaimImageDto>> GetImagesByClaimAsync(string claimId);
		Task DeleteWarrantyClaimImageAsync(string id);

		// Admin Methods
		Task<int> GetClaimCountByStatusAsync(string status);
		Task<IEnumerable<WarrantyClaimDto>> GetRecentClaimsAsync(int count = 10);
	}
}