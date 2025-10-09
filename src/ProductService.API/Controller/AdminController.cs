// API/Controllers/AccountController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ProductService.Business.DTOs;
using ProductService.Business.Interfaces;
using System;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Threading.Tasks;

namespace ProductService.API.Controllers
{
    [EnableCors("AllowAll")]
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin,SuperAdmin")] // Only admins can access these endpoints
    public class AccountController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IUserService _userService;
        private readonly ILogger<AccountController> _logger;

        public AccountController(
            IAuthService authService,
            IUserService userService,
            ILogger<AccountController> logger)
        {
            _authService = authService;
            _userService = userService;
            _logger = logger;
        }

        /// <summary>
        /// Create a new admin user (requires admin privileges)
        /// </summary>
        [HttpPost("create-admin")]
        public async Task<IActionResult> CreateAdmin([FromBody] AdminCreationRequest request)
        {
            try
            {
                _logger.LogInformation("Admin creation request received by user: {UserId}", User.FindFirstValue(ClaimTypes.NameIdentifier));

                if (!ModelState.IsValid)
                {
                    return BadRequest(new AuthResponse
                    {
                        Success = false,
                        Errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))
                    });
                }

                // Check if user already exists
                if (await _userService.UserExistsAsync(request.Email))
                {
                    return Conflict(new AuthResponse
                    {
                        Success = false,
                        Message = "User already exists with this email"
                    });
                }

                // Create admin registration DTO
                var registrationDto = new UserRegistrationDto
                {
                    FirstName = request.FirstName,
                    LastName = request.LastName,
                    Email = request.Email,
                    Password = request.Password,
                    Role = request.Role // This will be validated in AuthService
                };

                // Register the admin user
                var authResult = await _authService.RegisterAsync(registrationDto);

                if (!authResult.Success)
                {
                    return BadRequest(new AuthResponse
                    {
                        Success = false,
                        Errors = authResult.Errors
                    });
                }

                _logger.LogInformation("Admin user created successfully: {Email}", request.Email);

                return Ok(new AuthResponse
                {
                    Success = true,
                    Message = "Admin user created successfully",
                    User = authResult.User
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating admin user");
                return StatusCode(500, new AuthResponse
                {
                    Success = false,
                    Errors = new[] { "An error occurred while creating admin user" }
                });
            }
        }

        /// <summary>
        /// Get all users (admin only)
        /// </summary>
        [HttpGet("users")]
        public async Task<IActionResult> GetAllUsers()
        {
            try
            {
                // In a real implementation, you would have a method to get all users
                // For now, we'll return a placeholder response
                return Ok(new { message = "This endpoint would return all users" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving users");
                return StatusCode(500, new { message = "An error occurred while retrieving users" });
            }
        }

        /// <summary>
        /// Update user role (super admin only)
        /// </summary>
        [HttpPut("users/{userId}/role")]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> UpdateUserRole(string userId, [FromBody] UpdateRoleRequest request)
        {
            try
            {
                var result = await _userService.UpdateUserRoleAsync(userId, request.Role);
                if (!result)
                    return NotFound(new { message = "User not found" });

                _logger.LogInformation("User {UserId} role updated to {Role} by {AdminId}",
                    userId, request.Role, User.FindFirstValue(ClaimTypes.NameIdentifier));

                return Ok(new { message = $"User role updated to {request.Role}" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user role");
                return StatusCode(500, new { message = "An error occurred while updating user role" });
            }
        }

        [HttpPut("users/{userId}/role/test")]
        public IActionResult TestRoute(string userId)
        {
            return Ok(new { message = "Route works", userId });
        }
        /// <summary>
        /// Delete user (super admin only)
        /// </summary>
        [HttpDelete("users/{userId}")]
        [Authorize(Roles = "SuperAdmin")] // Only super admins can delete users
        public async Task<IActionResult> DeleteUser(string userId)
        {
            try
            {
                // In a real implementation, you would delete the user
                // For now, we'll return a placeholder response
                _logger.LogInformation("User {UserId} deleted by {AdminId}",
                    userId, User.FindFirstValue(ClaimTypes.NameIdentifier));

                return Ok(new { message = "User deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user");
                return StatusCode(500, new { message = "An error occurred while deleting user" });
            }
        }
    }

    // DTO for admin creation request
    public class AdminCreationRequest
    {
        [Required]
        public string FirstName { get; set; }

        [Required]
        public string LastName { get; set; }

        [Required, EmailAddress]
        public string Email { get; set; }

        [Required, MinLength(6)]
        public string Password { get; set; }

        [Required]
        [RegularExpression("Admin|SuperAdmin", ErrorMessage = "Role must be either 'Admin' or 'SuperAdmin'")]
        public string Role { get; set; } = "Admin";
    }

    // DTO for role update request
    public class UpdateRoleRequest
    {
        [Required]
        [RegularExpression("User|Admin|SuperAdmin", ErrorMessage = "Role must be 'User', 'Admin', or 'SuperAdmin'")]
        public string Role { get; set; }
    }
}