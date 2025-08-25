// ProductService.Business/Services/AuthService.cs
using AutoMapper;
using Microsoft.Extensions.Logging;
using ProductService.Business.DTOs;
using ProductService.Business.Interfaces;
using ProductService.Domain.Entites;
using ProductService.Domain.Entities;
using ProductService.Domain.Interfaces;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using UserService.Domain.Entities;
using IPasswordHasher = ProductService.Business.DTOs.IPasswordHasher;

namespace ProductService.Business.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly DTOs.IPasswordHasher _passwordHasher;
        private readonly IJwtTokenGenerator _jwtTokenGenerator;
        private readonly IEmailService _emailService;
        private readonly IMapper _mapper;
        private readonly ILogger<AuthService> _logger;

        public AuthService(
            IUserRepository userRepository,
            IPasswordHasher passwordHasher,
            IJwtTokenGenerator jwtTokenGenerator,
            IEmailService emailService,
            IMapper mapper,
            ILogger<AuthService> logger)
        {
            _userRepository = userRepository;
            _passwordHasher = passwordHasher;
            _jwtTokenGenerator = jwtTokenGenerator;
            _emailService = emailService;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<AuthResult> RegisterAsync(UserRegistrationDto registrationDto)
        {
            if (!string.IsNullOrEmpty(registrationDto.Role) &&
      !new[] { "User", "Admin", "SuperAdmin" }.Contains(registrationDto.Role))
            {
                return new AuthResult
                {
                    Success = false,
                    Message = "Invalid role specified",
                    Errors = new[] { "Role must be either 'User', 'Admin', or 'SuperAdmin'" }
                };
            }
            // Check if user already exists
            if (await _userRepository.UserExistsAsync(registrationDto.Email))
            {
                return new AuthResult
                {
                    Success = false,
                    Message = "User already exists with this email",
                    Errors = new[] { "User already exists" }
                };
            }

            // Create user
            var user = _mapper.Map<User>(registrationDto);
            user.Role = registrationDto.Role;

            // Hash password
            var passwordHash = _passwordHasher.HashPassword(registrationDto.Password);
            user.PasswordHash = passwordHash.Hash;
            user.PasswordSalt = passwordHash.Salt;

            // Set default preferences
            user.Preferences = new UserPreferences
            {
                EmailNotifications = true,
                SmsNotifications = false,
                Language = "en",
                Currency = "USD",
                Theme = "System"
            };

            await _userRepository.CreateAsync(user);

            // Generate tokens
            var token = _jwtTokenGenerator.GenerateToken(user);
            var refreshToken = _jwtTokenGenerator.GenerateRefreshToken();

            await _userRepository.AddRefreshTokenAsync(user.Id, refreshToken, DateTime.UtcNow.AddDays(7));

            return new AuthResult
            {
                Success = true,
                Token = token,
                RefreshToken = refreshToken,
                User = _mapper.Map<UserDto>(user)
            };
        }

        public async Task<AuthResult> LoginAsync(LoginRequestDto loginRequest)
        {
            try
            {
                var user = await _userRepository.GetByEmailAsync(loginRequest.Email);
                if (user == null)
                {
                    _logger.LogWarning($"Login failed: User with email {loginRequest.Email} not found");
                    return new AuthResult
                    {
                        Success = false,
                        Message = "Invalid credentials",
                        Errors = new[] { "Invalid credentials" }
                    };
                }

                // Debug logging to help identify the issue
                _logger.LogDebug($"Stored hash: {user.PasswordHash}");
                _logger.LogDebug($"Stored salt: {user.PasswordSalt}");

                bool isPasswordValid = _passwordHasher.VerifyPassword(
                    user.PasswordHash,
                    loginRequest.Password,
                    user.PasswordSalt);

                _logger.LogDebug($"Password verification result: {isPasswordValid}");

                if (!isPasswordValid)
                {
                    _logger.LogWarning($"Login failed: Invalid password for user {user.Email}");
                    return new AuthResult
                    {
                        Success = false,
                        Message = "Invalid credentials",
                        Errors = new[] { "Invalid credentials" }
                    };
                }

                // Update last login
                user.LastLogin = DateTime.UtcNow;
                await _userRepository.UpdateAsync(user);

                // Generate tokens
                var token = _jwtTokenGenerator.GenerateToken(user);
                var refreshToken = _jwtTokenGenerator.GenerateRefreshToken();

                await _userRepository.AddRefreshTokenAsync(user.Id, refreshToken, DateTime.UtcNow.AddDays(7));

                return new AuthResult
                {
                    Success = true,
                    Token = token,
                    RefreshToken = refreshToken,
                    User = _mapper.Map<UserDto>(user)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login process");
                return new AuthResult
                {
                    Success = false,
                    Message = "An error occurred during login",
                    Errors = new[] { "An error occurred during login" }
                };
            }
        }
        public async Task<AuthResult> RefreshTokenAsync(string token, string refreshToken)
        {
            var principal = _jwtTokenGenerator.GetPrincipalFromExpiredToken(token);
            var userId = principal.Claims.FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                return new AuthResult
                {
                    Success = false,
                    Message = "Invalid token"
                };
            }

            var user = await _userRepository.GetUserByRefreshTokenAsync(refreshToken);
            if (user == null || user.Id != userId)
            {
                return new AuthResult
                {
                    Success = false,
                    Message = "Invalid refresh token"
                };
            }

            var newToken = _jwtTokenGenerator.GenerateToken(user);
            var newRefreshToken = _jwtTokenGenerator.GenerateRefreshToken();

            await _userRepository.AddRefreshTokenAsync(user.Id, newRefreshToken, DateTime.UtcNow.AddDays(7));

            return new AuthResult
            {
                Success = true,
                Token = newToken,
                RefreshToken = newRefreshToken,
                User = _mapper.Map<UserDto>(user)
            };
        }

        public async Task<bool> RevokeTokenAsync(string userId)
        {
            await _userRepository.RevokeRefreshTokenAsync(userId);
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

        public async Task<bool> RequestPasswordResetAsync(string email)
        {
            var user = await _userRepository.GetByEmailAsync(email);
            if (user == null)
                return true; // Don't reveal that the user doesn't exist

            // Generate reset token (this could be a JWT or a random string)
            var resetToken = _jwtTokenGenerator.GenerateRefreshToken(); // Using refresh token as an example

            // In a real application, you would store the reset token with an expiry and send an email
            // For now, we'll just log it
            // await _emailService.SendPasswordResetEmailAsync(user.Email, resetToken);

            return true;
        }

        public async Task<bool> ResetPasswordAsync(ResetPasswordDto resetPasswordDto)
        {
            // Validate the reset token (this is a simplified example)
            // In a real application, you would have a table for reset tokens and validate against it

            var user = await _userRepository.GetByEmailAsync(resetPasswordDto.Email);
            if (user == null)
                return false;

            // Hash new password
            var newPasswordHash = _passwordHasher.HashPassword(resetPasswordDto.NewPassword);
            user.PasswordHash = newPasswordHash.Hash;
            user.PasswordSalt = newPasswordHash.Salt;

            await _userRepository.UpdateAsync(user);
            return true;
        }
    }
}