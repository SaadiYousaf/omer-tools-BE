// ProductService.Business/Services/AuthService.cs
using AutoMapper;
using Google.Apis.Auth;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.SqlServer.Server;
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
        private readonly IConfiguration _configuration;


        public AuthService(
            IUserRepository userRepository,
            IPasswordHasher passwordHasher,
            IJwtTokenGenerator jwtTokenGenerator,
            IEmailService emailService,
            IMapper mapper,
            ILogger<AuthService> logger,
            IConfiguration configuration)
        {
            _userRepository = userRepository;
            _passwordHasher = passwordHasher;
            _jwtTokenGenerator = jwtTokenGenerator;
            _emailService = emailService;
            _mapper = mapper;
            _logger = logger;
            _configuration = configuration;
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
                if (user.AuthProvider == "Google" && !string.IsNullOrEmpty(user.GoogleId))
                {
                    return new AuthResult
                    {
                        Success = false,
                        Message = "This account uses Google authentication. Please sign in with Google.",
                        Errors = new[] { "Use Google Sign-In for this account" }
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

        public async Task<AuthResult> GoogleSignInAsync(GoogleAuthRequestDto googleAuth)
        {
            try
            {
                // Validate Google token and get user info
                var googleUser = await ValidateGoogleTokenAsync(googleAuth.IdToken);

                if (googleUser == null || string.IsNullOrEmpty(googleUser.Email))
                {
                    return new AuthResult
                    {
                        Success = false,
                        Message = "Invalid Google token",
                        Errors = new[] { "Invalid Google token" }
                    };
                }

                // Check if user exists
                var user = await _userRepository.GetByEmailAsync(googleUser.Email);

                if (user == null)
                {
                    // Create new user from Google info
                    user = new User
                    {
                        Email = googleUser.Email,
                        FirstName = googleUser.FirstName ?? googleUser.Name?.Split(' ').FirstOrDefault(),
                        LastName = googleUser.LastName ?? googleUser.Name?.Split(' ').LastOrDefault(),
                        Role = "User", // Default role
                        IsEmailVerified = googleUser.VerifiedEmail,
                        AuthProvider = "Google",
                        GoogleId = googleUser.Id,
                        CreatedAt = DateTime.UtcNow,
                        LastLogin = DateTime.UtcNow
                    };
                    // Set dummy password fields for Google users
                    var dummyPassword = _passwordHasher.HashPassword(Guid.NewGuid().ToString());
                    user.PasswordHash = dummyPassword.Hash;
                    user.PasswordSalt = dummyPassword.Salt;


                    // Set default preferences
                    user.Preferences = new UserPreferences
                    {
                        EmailNotifications = true,
                        SmsNotifications = false,
                        Language = "en",
                        Currency = "AUD",
                        Theme = "System"
                    };

                    await _userRepository.CreateAsync(user);
                }
                else
                {
                    if (string.IsNullOrEmpty(user.PasswordHash) || string.IsNullOrEmpty(user.PasswordSalt))
                    {
                        var dummyPassword = _passwordHasher.HashPassword(Guid.NewGuid().ToString());
                        user.PasswordHash = dummyPassword.Hash;
                        user.PasswordSalt = dummyPassword.Salt;
                        _logger.LogInformation($"Set dummy password for existing user {user.Email}");
                    }

                    // Scenario 1: Convert local user to also allow Google login
                    if (string.IsNullOrEmpty(user.GoogleId))
                    {
                        user.GoogleId = googleUser.Id;
                        user.AuthProvider = "Google"; // Or keep as "Local" if you want both
                        _logger.LogInformation($"Added Google authentication to existing user {user.Email}");
                    }

                    // Update last login for all scenarios
                    user.LastLogin = DateTime.UtcNow;
                    await _userRepository.UpdateAsync(user);
                }

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
                _logger.LogError(ex, "Error during Google sign-in");
                return new AuthResult
                {
                    Success = false,
                    Message = "An error occurred during Google sign-in",
                    Errors = new[] { "An error occurred during Google sign-in" }
                };
            }
        }

        private async Task<GoogleUserInfoDto> ValidateGoogleTokenAsync(string idToken)
        {
            try
            {
                var settings = new GoogleJsonWebSignature.ValidationSettings()
                {
                    Audience = new[] { _configuration["Google:ClientId"] }
                };

                var payload = await GoogleJsonWebSignature.ValidateAsync(idToken, settings);

                return new GoogleUserInfoDto
                {
                    Id = payload.Subject,
                    Email = payload.Email,
                    Name = payload.Name,
                    FirstName = payload.GivenName,
                    LastName = payload.FamilyName,
                    Picture = payload.Picture,
                    VerifiedEmail = payload.EmailVerified
                };
            }
            catch (InvalidJwtException ex)
            {
                _logger.LogWarning(ex, "Invalid Google token");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating Google token");
                return null;
            }
        }
    }
}