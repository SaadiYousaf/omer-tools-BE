using System.Threading.Tasks;

namespace ProductService.Business.Interfaces
{
    public interface IStockService
    {
        Task<bool> ReserveStockAsync(string productId, int quantity);
        Task<bool> ReleaseStockAsync(string productId, int quantity);
        Task<bool> UpdateStockAsync(string productId, int quantityChange);
    }
}