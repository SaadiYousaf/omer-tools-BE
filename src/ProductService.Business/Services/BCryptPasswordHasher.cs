// Business/Services/BCryptPasswordHasher.cs
using BCrypt.Net;
using Org.BouncyCastle.Crypto.Generators;
using ProductService.Business.Interfaces;

namespace ProductService.Business.Services
{
    public class BCryptPasswordHasher : IPasswordHasher
    {
        public string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password, BCrypt.Net.BCrypt.GenerateSalt(12));
        }

        public bool VerifyPassword(string hashedPassword, string providedPassword)
        {
            return BCrypt.Net.BCrypt.Verify(providedPassword, hashedPassword);
        }
    }
}