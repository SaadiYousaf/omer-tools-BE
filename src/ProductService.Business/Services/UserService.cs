// ProductService.Business/Services/UserService.cs
using AutoMapper;
using ProductService.Business.DTOs;
using ProductService.Business.Interfaces;

namespace ProductService.Business.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly DTOs.IPasswordHasher _passwordHasher;
        private readonly IMapper _mapper;

        public UserService(
            IUserRepository userRepository,
            DTOs.IPasswordHasher passwordHasher,
            IMapper mapper)
        {
            _userRepository = userRepository;
            _passwordHasher = passwordHasher;
            _mapper = mapper;
        }

        public async Task<UserDto> GetUserProfileAsync(string userId)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            return _mapper.Map<UserDto>(user);
        }

        public async Task<bool> UpdateProfileAsync(string userId, UpdateProfileDto updateProfileDto)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
                return false;

            // Update the user properties
            user.FirstName = updateProfileDto.FirstName ?? user.FirstName;
            user.LastName = updateProfileDto.LastName ?? user.LastName;
            user.PhoneNumber = updateProfileDto.PhoneNumber ?? user.PhoneNumber;
            user.UpdatedAt = System.DateTime.UtcNow;

            await _userRepository.UpdateAsync(user);
            return true;
        }

        public async Task<bool> ChangePasswordAsync(string userId, ChangePasswordDto changePasswordDto)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
                return false;

            // Verify current password
            if (!_passwordHasher.VerifyPassword(user.PasswordHash, changePasswordDto.CurrentPassword, user.PasswordSalt))
                return false;

            // Hash new password
            var newPasswordHash = _passwordHasher.HashPassword(changePasswordDto.NewPassword);
            user.PasswordHash = newPasswordHash.Hash;
            user.PasswordSalt = newPasswordHash.Salt;

            await _userRepository.UpdateAsync(user);
            return true;
        }

        public async Task<bool> DeleteAccountAsync(string userId)
        {
            return await _userRepository.DeleteAsync(userId);
        }
    }
}
