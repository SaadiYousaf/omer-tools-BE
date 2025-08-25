using ProductService.Business.DTOs;

public interface IAuthService
{
    Task<AuthResult> RegisterAsync(UserRegistrationDto registrationDto);
    Task<AuthResult> LoginAsync(LoginRequestDto loginRequest);
    Task<AuthResult> RefreshTokenAsync(string token, string refreshToken);
    Task<bool> RevokeTokenAsync(string userId);
    Task<bool> ChangePasswordAsync(string userId, ChangePasswordDto changePasswordDto);
    Task<bool> RequestPasswordResetAsync(string email);
    Task<bool> ResetPasswordAsync(ResetPasswordDto resetPasswordDto);
}

public class AuthResult
{
    public bool Success { get; set; }
    public string Token { get; set; }
    public string RefreshToken { get; set; }
    public UserDto User { get; set; }
    public string Message { get; set; }
    public IEnumerable<string> Errors { get; set; }
}