// API/Controllers/AuthController.cs
using Microsoft.AspNetCore.Mvc;
using ProductService.Business.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace ProductService.API.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var result = await _authService.Authenticate(request.Email, request.Password);

            if (!result.Success)
                return BadRequest(new { message = result.Message });

            return Ok(new
            {
                token = result.Token,
                user = new
                {
                    id = result.User.Id,
                    email = result.User.Email,
                    firstName = result.User.FirstName,
                    lastName = result.User.LastName
                }
            });
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            var result = await _authService.Register(
                request.FirstName,
                request.LastName,
                request.Email,
                request.Password
            );

            if (!result.Success)
                return BadRequest(new { message = result.Message });

            return Ok(new
            {
                token = result.Token,
                user = new
                {
                    id = result.User.Id,
                    email = result.User.Email,
                    firstName = result.User.FirstName,
                    lastName = result.User.LastName
                }
            });
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