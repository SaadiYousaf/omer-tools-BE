// ProductService.Business/Interfaces/IUserService.cs
using ProductService.Business.DTOs;
using System.Threading.Tasks;

namespace ProductService.Business.Interfaces
{
    public interface IUserService
    {
        Task<UserDto> GetUserProfileAsync(string userId);
        Task<bool> UpdateProfileAsync(string userId, UpdateProfileDto updateProfileDto);
        Task<bool> DeleteAccountAsync(string userId);
        Task<bool> UserExistsAsync(string email);

        // Note: ChangePasswordAsync is typically handled by IAuthService
        // but if you want it in IUserService as well, you can add it:
        Task<bool> ChangePasswordAsync(string userId, ChangePasswordDto changePasswordDto);
    }
}