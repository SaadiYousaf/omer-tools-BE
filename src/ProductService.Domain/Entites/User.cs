// UserService.Domain/Entities/User.cs
using ProductService.Domain.Entites;
using ProductService.Domain.Entities;

namespace UserService.Domain.Entities
{
    public class User : BaseEntity
    {
        public User()
        {
            CreatedAt = DateTime.UtcNow;
            IsActive = true;
            Addresses = new List<Address>();
            PaymentMethods = new List<PaymentMethod>();
        }

        public string Email { get; set; }
        public string PasswordHash { get; set; }
        public string PasswordSalt { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string PhoneNumber { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? LastLogin { get; set; }
        public string RefreshToken { get; set; }
        public DateTime? RefreshTokenExpiry { get; set; }

        // Navigation properties
        public ICollection<Order> Orders { get; set; } = new List<Order>();
        public ICollection<Address> Addresses { get; set; }
        public ICollection<PaymentMethod> PaymentMethods { get; set; }
        public UserPreferences Preferences { get; set; }
    }
}
