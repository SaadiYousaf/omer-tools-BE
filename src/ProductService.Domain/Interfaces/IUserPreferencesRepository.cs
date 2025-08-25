// ProductService.Domain/Interfaces/IUserPreferencesRepository.cs
using ProductService.Domain.Entites;
using ProductService.Domain.Entities;
using System.Threading.Tasks;

namespace ProductService.Domain.Interfaces
{
    public interface IUserPreferencesRepository
    {
        Task<UserPreferences> GetByUserIdAsync(string userId);
        Task CreateAsync(UserPreferences preferences);
        Task UpdateAsync(UserPreferences preferences);
    }
}