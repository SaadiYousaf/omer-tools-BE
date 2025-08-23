// Business/Interfaces/IAuthService.cs
using ProductService.Domain.Entites;
using ProductService.Domain.Entities;
using System.Threading.Tasks;

namespace ProductService.Business.Interfaces
{
    public class AuthResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string Token { get; set; }
        public User User { get; set; }
    }

    public interface IAuthService
    {
        Task<AuthResult> Authenticate(string email, string password);
        Task<AuthResult> Register(string firstName, string lastName, string email, string password);
        string GenerateJwtToken(User user);
    }
}