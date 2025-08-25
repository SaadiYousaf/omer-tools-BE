using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProductService.Business.DTOs
{
    // ProductService.Common/Security/PasswordHasher.cs
    using System;
    using System.Security.Cryptography;
    using System.Text;

        public interface IPasswordHasher
        {
            PasswordHashResult HashPassword(string password);
            bool VerifyPassword(string storedHash, string providedPassword, string salt);
        }

        public class PasswordHashResult
        {
            public string Hash { get; set; }
            public string Salt { get; set; }
        }

        public class PasswordHasher : IPasswordHasher
        {
            public PasswordHashResult HashPassword(string password)
            {
                // Generate a random salt
                var saltBytes = new byte[16];
                using (var rng = RandomNumberGenerator.Create())
                {
                    rng.GetBytes(saltBytes);
                }
                var salt = Convert.ToBase64String(saltBytes);

                // Hash the password with the salt
                var hash = ComputeHash(password, salt);

                return new PasswordHashResult
                {
                    Hash = hash,
                    Salt = salt
                };
            }

            public bool VerifyPassword(string storedHash, string providedPassword, string salt)
            {
                var computedHash = ComputeHash(providedPassword, salt);
                return storedHash == computedHash;
            }

            private string ComputeHash(string password, string salt)
            {
                using (var sha256 = SHA256.Create())
                {
                    var saltedPassword = password + salt;
                    var bytes = Encoding.UTF8.GetBytes(saltedPassword);
                    var hash = sha256.ComputeHash(bytes);
                    return Convert.ToBase64String(hash);
                }
            }
        }
    
}
