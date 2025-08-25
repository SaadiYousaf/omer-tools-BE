// API/Controllers/AuthController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ProductService.Business.DTOs;
using ProductService.Business.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Threading.Tasks;

namespace ProductService.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IAuthService authService, ILogger<AuthController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register([FromBody] UserRegistrationDto registrationDto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(new AuthResponse
                    {
                        Success = false,
                        Errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))
                    });

                var authResult = await _authService.RegisterAsync(registrationDto);

                if (!authResult.Success)
                    return BadRequest(new AuthResponse
                    {
                        Success = false,
                        Errors = authResult.Errors
                    });

                return Ok(new AuthResponse
                {
                    Success = true,
                    Token = authResult.Token,
                    RefreshToken = authResult.RefreshToken,
                    User = authResult.User
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during user registration");
                return StatusCode(500, new AuthResponse
                {
                    Success = false,
                    Errors = new[] { "An error occurred during registration" }
                });
            }
        }
        [HttpPost("register-admin")]
        [Authorize(Roles = "SuperAdmin")] 
        public async Task<IActionResult> RegisterAdmin([FromBody] UserRegistrationDto registrationDto)
        {
            registrationDto.Role = "Admin"; 
            return await Register(registrationDto);
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto loginRequest)
        {
            try
            {
                _logger.LogInformation($"Login attempt for email: {loginRequest.Email}");

                if (!ModelState.IsValid)
                    return BadRequest(new AuthResponse
                    {
                        Success = false,
                        Errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))
                    });

                var authResult = await _authService.LoginAsync(loginRequest);

                if (!authResult.Success)
                {
                    _logger.LogWarning($"Login failed for email: {loginRequest.Email}, Reason: {authResult.Message}");
                    return BadRequest(new AuthResponse
                    {
                        Success = false,
                        Errors = authResult.Errors
                    });
                }

                _logger.LogInformation($"Login successful for email: {loginRequest.Email}");
                return Ok(new AuthResponse
                {
                    Success = true,
                    Token = authResult.Token,
                    RefreshToken = authResult.RefreshToken,
                    User = authResult.User
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during user login");
                return StatusCode(500, new AuthResponse
                {
                    Success = false,
                    Errors = new[] { "An error occurred during login" }
                });
            }
        }
        [HttpPost("refresh-token")]
        [AllowAnonymous]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
        {
            var authResult = await _authService.RefreshTokenAsync(request.Token, request.RefreshToken);

            if (!authResult.Success)
                return BadRequest(new AuthResponse { Errors = new[] { "Invalid tokens" } });

            return Ok(new AuthResponse
            {
                Success = true,
                Token = authResult.Token,
                RefreshToken = authResult.RefreshToken,
                User = authResult.User
            });
        }

        [HttpPost("revoke-token")]
        [Authorize]
        public async Task<IActionResult> RevokeToken()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var result = await _authService.RevokeTokenAsync(userId);

            if (!result)
                return BadRequest(new { message = "Unable to revoke token" });

            return Ok(new { message = "Token revoked successfully" });
        }
    }

    public class LoginRequest
    {
        [Required, EmailAddress]
        public string Email { get; set; }

        [Required]
        public string Password { get; set; }
    }

    public class RegisterRequest
    {
        [Required] public string FirstName { get; set; }
        [Required] public string LastName { get; set; }
        [Required, EmailAddress] public string Email { get; set; }
        [Required] public string Password { get; set; }
    }
}