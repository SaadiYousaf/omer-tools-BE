// Business/Services/AuthService.cs
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using ProductService.Business.Interfaces;
using ProductService.Domain.Entites;
using ProductService.Domain.Entities;
using ProductService.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace ProductService.Business.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IConfiguration _config;

        public AuthService(
            IUserRepository userRepository,
            IPasswordHasher passwordHasher,
            IConfiguration config)
        {
            _userRepository = userRepository;
            _passwordHasher = passwordHasher;
            _config = config;
        }

        public async Task<AuthResult> Authenticate(string email, string password)
        {
            var user = await _userRepository.GetUserByEmailAsync(email);
            if (user == null)
                return new AuthResult { Success = false, Message = "User not found" };

            if (!_passwordHasher.VerifyPassword(user.PasswordHash, password))
                return new AuthResult { Success = false, Message = "Invalid password" };

            var token = GenerateJwtToken(user);

            return new AuthResult
            {
                Success = true,
                Token = token,
                User = user
            };
        }

        public async Task<AuthResult> Register(string firstName, string lastName, string email, string password)
        {
            if (await _userRepository.UserExists(email))
                return new AuthResult { Success = false, Message = "Email already registered" };

            var user = new User
            {
                Id = Guid.NewGuid().ToString(),
                FirstName = firstName,
                LastName = lastName,
                Email = email,
                PasswordHash = _passwordHasher.HashPassword(password)
            };

            await _userRepository.CreateUserAsync(user);
            var token = GenerateJwtToken(user);

            return new AuthResult
            {
                Success = true,
                Token = token,
                User = user
            };
        }

        // Business/Services/AuthService.cs
        public string GenerateJwtToken(User user)
        {
            var claims = new List<Claim>
    {
        new Claim(JwtRegisteredClaimNames.Sub, user.Email),
        new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        new Claim(ClaimTypes.NameIdentifier, user.Id), // Use user.Id (GUID) instead of user.Id.ToString()
        new Claim(ClaimTypes.Email, user.Email), // Add email claim
        new Claim(ClaimTypes.GivenName, user.FirstName),
        new Claim(ClaimTypes.Surname, user.LastName)
    };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expires = DateTime.Now.AddDays(Convert.ToDouble(_config["Jwt:ExpireDays"]));

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: expires,
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}