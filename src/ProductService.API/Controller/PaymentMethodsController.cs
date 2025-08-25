// ProductService.API/Controllers/PaymentMethodsController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProductService.Business.DTOs;
using ProductService.Business.Interfaces;
using Stripe;
using System.Security.Claims;
using System.Threading.Tasks;

namespace ProductService.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PaymentMethodsController : ControllerBase
    {
        private readonly IPaymentMethodService _paymentMethodService;

        public PaymentMethodsController(IPaymentMethodService paymentMethodService)
        {
            _paymentMethodService = paymentMethodService;
        }

        [HttpGet]
        public async Task<IActionResult> GetUserPaymentMethods()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var paymentMethods = await _paymentMethodService.GetUserPaymentMethodsAsync(userId);
            return Ok(paymentMethods);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetPaymentMethod(string id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var paymentMethod = await _paymentMethodService.GetPaymentMethodByIdAsync(id);

            // We need to check if the payment method belongs to the user
            // Since the service returns a DTO, we need to check the UserId in the DTO
            if (paymentMethod == null || paymentMethod.UserId != userId)
                return NotFound();

            return Ok(paymentMethod);
        }

        [HttpPost]
        public async Task<IActionResult> CreatePaymentMethod([FromBody] CreatePaymentMethodDto paymentMethodDto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var paymentMethod = await _paymentMethodService.CreatePaymentMethodAsync(userId, paymentMethodDto);
            return CreatedAtAction(nameof(GetPaymentMethod), new { id = paymentMethod.Id }, paymentMethod);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdatePaymentMethod(string id, [FromBody] UpdatePaymentMethodDto paymentMethodDto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var paymentMethod = await _paymentMethodService.UpdatePaymentMethodAsync(userId, id, paymentMethodDto);

            if (paymentMethod == null)
                return NotFound();

            return Ok(paymentMethod);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePaymentMethod(string id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var result = await _paymentMethodService.DeletePaymentMethodAsync(userId, id);

            if (!result)
                return NotFound();

            return NoContent();
        }

        [HttpPost("{id}/set-default")]
        public async Task<IActionResult> SetDefaultPaymentMethod(string id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            await _paymentMethodService.SetDefaultPaymentMethodAsync(userId, id);
            return Ok();
        }
    }
}