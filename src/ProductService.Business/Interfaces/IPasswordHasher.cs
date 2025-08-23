// Business/Interfaces/IPasswordHasher.cs
namespace ProductService.Business.Interfaces
{
    public interface IPasswordHasher
    {
        string HashPassword(string password);
        bool VerifyPassword(string hashedPassword, string providedPassword);
    }
}