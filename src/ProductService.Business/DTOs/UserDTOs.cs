using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProductService.Business.DTOs
{
    public class CreateAddressDto
    {
        [Required]
        public string AddressType { get; set; }

        [Required]
        public string FullName { get; set; }

        [Required]
        public string AddressLine1 { get; set; }

        public string AddressLine2 { get; set; }

        [Required]
        public string City { get; set; }

        [Required]
        public string State { get; set; }

        [Required]
        public string PostalCode { get; set; }

        [Required]
        public string Country { get; set; }

        public bool IsDefault { get; set; }

        public string PhoneNumber { get; set; }
    }
    public class UpdateAddressDto
    {
        public string AddressType { get; set; }
        public string FullName { get; set; }
        public string AddressLine1 { get; set; }
        public string AddressLine2 { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string PostalCode { get; set; }
        public string Country { get; set; }
        public bool IsDefault { get; set; }
        public string PhoneNumber { get; set; }
    }
    public class CreatePaymentMethodDto
    {
        [Required]
        public string PaymentType { get; set; }

        [Required]
        public string Provider { get; set; }

        [Required]
        public string Last4Digits { get; set; }

        [Required]
        public string ExpiryMonth { get; set; }

        [Required]
        public string ExpiryYear { get; set; }

        public bool IsDefault { get; set; }

        public string PaymentMethodId { get; set; } // From payment processor
    }

    public class UpdatePaymentMethodDto
    {
        public string PaymentType { get; set; }
        public string Provider { get; set; }
        public string Last4Digits { get; set; }
        public string ExpiryMonth { get; set; }
        public string ExpiryYear { get; set; }
        public bool IsDefault { get; set; }
        public string PaymentMethodId { get; set; }
    }
    public class UpdateUserPreferencesDto
    {
        public bool EmailNotifications { get; set; }
        public bool SmsNotifications { get; set; }
        public string Language { get; set; }
        public string Currency { get; set; }
        public string Theme { get; set; }
    }
    public class UserRegistrationDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [StringLength(100, MinimumLength = 6)]
        public string Password { get; set; }

        [Required]
        public string FirstName { get; set; }

        [Required]
        public string LastName { get; set; }

        public string PhoneNumber { get; set; }

        public string address { get; set; }
        public string Role { get; set; } = "User";


    }
    // GoogleAuthRequestDto.cs
    public class GoogleAuthRequestDto
    {
        [Required]
        public string IdToken { get; set; }
    }

    // GoogleUserInfoDto.cs
    public class GoogleUserInfoDto
    {
        public string Id { get; set; }
        public string Email { get; set; }
        public string Name { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Picture { get; set; }
        public bool VerifiedEmail { get; set; }
    }
    public class LoginRequestDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        public string Password { get; set; }
    }

    public class UserDto
    {
        public string Id { get; set; }
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string PhoneNumber { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastLogin { get; set; }
        public UserPreferencesDto Preferences { get; set; }
        public List<AddressDto> Addresses { get; set; }
        public List<PaymentMethodDto> PaymentMethods { get; set; }
    }

    public class UpdateProfileDto
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string PhoneNumber { get; set; }
    }

    public class ChangePasswordDto
    {
        [Required]
        public string CurrentPassword { get; set; }

        [Required]
        [StringLength(100, MinimumLength = 6)]
        public string NewPassword { get; set; }
    }

    public class AuthResponse
    {
        public bool Success { get; set; }
        public string Token { get; set; }
        public string RefreshToken { get; set; }
        public UserDto User { get; set; }
        public IEnumerable<string> Errors { get; set; }
        public string Message { get; set; }
    }

    public class RefreshTokenRequest
    {
        public string Token { get; set; }
        public string RefreshToken { get; set; }
    }
    public class AddressDto
    {
        public string Id { get; set; }
        public string UserId { get; set; }
        public string AddressType { get; set; }
        public string FullName { get; set; }
        public string AddressLine1 { get; set; }
        public string AddressLine2 { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string PostalCode { get; set; }
        public string Country { get; set; }
        public bool IsDefault { get; set; }
        public string PhoneNumber { get; set; }
    }

    public class PaymentMethodDto
    {
        public string Id { get; set; }
        public string UserId { get; set; }
        public string PaymentType { get; set; }
        public string Provider { get; set; }
        public string Last4Digits { get; set; }
        public string ExpiryMonth { get; set; }
        public string ExpiryYear { get; set; }
        public bool IsDefault { get; set; }
    }

    public class ResetPasswordDto
    {
        [Required]
        public string Email { get; set; }

        [Required]
        public string Token { get; set; }

        [Required]
        [StringLength(100, MinimumLength = 6)]
        public string NewPassword { get; set; }
    }

    public class UserPreferencesDto
    {
        public bool EmailNotifications { get; set; }
        public bool SmsNotifications { get; set; }
        public string Language { get; set; }
        public string Currency { get; set; }
        public string Theme { get; set; }
    }

}
