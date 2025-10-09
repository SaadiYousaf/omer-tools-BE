// ProductService.API/Controllers/AddressesController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using ProductService.Business.DTOs;
using ProductService.Business.Interfaces;
using System.Security.Claims;
using System.Threading.Tasks;

namespace ProductService.API.Controllers
{
    [EnableCors("AllowAll")]
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class AddressesController : ControllerBase
    {
        private readonly IAddressService _addressService;

        public AddressesController(IAddressService addressService)
        {
            _addressService = addressService;
        }

        [HttpGet]
        public async Task<IActionResult> GetUserAddresses()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var addresses = await _addressService.GetUserAddressesAsync(userId);
            return Ok(addresses);
        }

     
        [HttpGet("{id}")]
        public async Task<IActionResult> GetAddress(string id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var address = await _addressService.GetAddressByIdAsync(id);

            // We need to check if the address belongs to the user
            // Since the service returns a DTO, we need to check the UserId in the DTO
            if (address == null || address.UserId != userId)
                return NotFound();

            return Ok(address);
        }

        [HttpPost]
        public async Task<IActionResult> CreateAddress([FromBody] CreateAddressDto addressDto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var address = await _addressService.CreateAddressAsync(userId, addressDto);
            return CreatedAtAction(nameof(GetAddress), new { id = address.Id }, address);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateAddress(string id, [FromBody] UpdateAddressDto addressDto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var address = await _addressService.UpdateAddressAsync(userId, id, addressDto);

            if (address == null)
                return NotFound();

            return Ok(address);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAddress(string id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var result = await _addressService.DeleteAddressAsync(userId, id);

            if (!result)
                return NotFound();

            return NoContent();
        }

        [HttpPost("{id}/set-default")]
        public async Task<IActionResult> SetDefaultAddress(string id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            await _addressService.SetDefaultAddressAsync(userId, id);
            return Ok();
        }
    }
}