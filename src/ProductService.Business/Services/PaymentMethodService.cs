
// ProductService.Business/Services/PaymentMethodService.cs
using AutoMapper;
using ProductService.Business.DTOs;
using ProductService.Business.Interfaces;
using ProductService.Domain.Entites;
using ProductService.Domain.Entities;
using ProductService.Domain.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ProductService.Business.Services
{
    public class PaymentMethodService : IPaymentMethodService
    {
        private readonly IPaymentMethodRepository _paymentMethodRepository;
        private readonly IMapper _mapper;

        public PaymentMethodService(IPaymentMethodRepository paymentMethodRepository, IMapper mapper)
        {
            _paymentMethodRepository = paymentMethodRepository;
            _mapper = mapper;
        }

        public async Task<PaymentMethodDto> GetPaymentMethodByIdAsync(string paymentMethodId)
        {
            var paymentMethod = await _paymentMethodRepository.GetByIdAsync(paymentMethodId);
            return _mapper.Map<PaymentMethodDto>(paymentMethod);
        }

        public async Task<IEnumerable<PaymentMethodDto>> GetUserPaymentMethodsAsync(string userId)
        {
            var paymentMethods = await _paymentMethodRepository.GetByUserIdAsync(userId);
            return _mapper.Map<IEnumerable<PaymentMethodDto>>(paymentMethods);
        }

        public async Task<PaymentMethodDto> CreatePaymentMethodAsync(string userId, CreatePaymentMethodDto paymentMethodDto)
        {
            var paymentMethod = _mapper.Map<PaymentMethod>(paymentMethodDto);
            paymentMethod.UserId = userId;

            await _paymentMethodRepository.CreateAsync(paymentMethod);
            return _mapper.Map<PaymentMethodDto>(paymentMethod);
        }

        public async Task<PaymentMethodDto> UpdatePaymentMethodAsync(string userId, string paymentMethodId, UpdatePaymentMethodDto paymentMethodDto)
        {
            var paymentMethod = await _paymentMethodRepository.GetByIdAsync(paymentMethodId);
            if (paymentMethod == null || paymentMethod.UserId != userId)
                return null;

            _mapper.Map(paymentMethodDto, paymentMethod);
            await _paymentMethodRepository.UpdateAsync(paymentMethod);

            return _mapper.Map<PaymentMethodDto>(paymentMethod);
        }

        public async Task<bool> DeletePaymentMethodAsync(string userId, string paymentMethodId)
        {
            var paymentMethod = await _paymentMethodRepository.GetByIdAsync(paymentMethodId);
            if (paymentMethod == null || paymentMethod.UserId != userId)
                return false;

            return await _paymentMethodRepository.DeleteAsync(paymentMethodId);
        }

        public async Task SetDefaultPaymentMethodAsync(string userId, string paymentMethodId)
        {
            await _paymentMethodRepository.SetDefaultPaymentMethodAsync(userId, paymentMethodId);
        }
    }
}