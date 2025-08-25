// ProductService.API/Controllers/PreferencesController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProductService.Business.DTOs;
using ProductService.Business.Interfaces;
using System.Security.Claims;
using System.Threading.Tasks;

namespace ProductService.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PreferencesController : ControllerBase
    {
        private readonly IUserPreferencesService _userPreferencesService;

        public PreferencesController(IUserPreferencesService userPreferencesService)
        {
            _userPreferencesService = userPreferencesService;
        }

        [HttpGet]
        public async Task<IActionResult> GetUserPreferences()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var preferences = await _userPreferencesService.GetUserPreferencesAsync(userId);
            return Ok(preferences);
        }

        [HttpPut]
        public async Task<IActionResult> UpdateUserPreferences([FromBody] UpdateUserPreferencesDto preferencesDto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var preferences = await _userPreferencesService.UpdateUserPreferencesAsync(userId, preferencesDto);
            return Ok(preferences);
        }
    }
}