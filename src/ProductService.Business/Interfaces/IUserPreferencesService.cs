// ProductService.Business/Interfaces/IUserPreferencesService.cs
using ProductService.Business.DTOs;
using ProductService.Business.Interfaces;
using ProductService.Domain.Entites;
using System.Threading.Tasks;

namespace ProductService.Business.Interfaces
{
    public interface IUserPreferencesService
    {
        Task<UserPreferencesDto> GetUserPreferencesAsync(string userId);
        Task<UserPreferencesDto> UpdateUserPreferencesAsync(string userId, UpdateUserPreferencesDto preferencesDto);
    }
}
