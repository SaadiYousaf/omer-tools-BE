

// ProductService.DataAccess/Repositories/PaymentMethodRepository.cs
using Microsoft.EntityFrameworkCore;
using ProductService.DataAccess.Data;
using ProductService.Domain.Entites;
using ProductService.Domain.Entities;
using ProductService.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ProductService.DataAccess.Repositories
{
    public class PaymentMethodRepository : IPaymentMethodRepository
    {
        private readonly ProductDbContext _context;

        public PaymentMethodRepository(ProductDbContext context)
        {
            _context = context;
        }

        public async Task<PaymentMethod> GetByIdAsync(string id)
        {
            return await _context.PaymentMethods
                .FirstOrDefaultAsync(p => p.Id == id && p.IsActive);
        }

        public async Task<IEnumerable<PaymentMethod>> GetByUserIdAsync(string userId)
        {
            return await _context.PaymentMethods
                .Where(p => p.UserId == userId && p.IsActive)
                .OrderByDescending(p => p.IsDefault)
                .ThenBy(p => p.PaymentType)
                .ToListAsync();
        }

        public async Task<PaymentMethod> GetDefaultPaymentMethodAsync(string userId)
        {
            return await _context.PaymentMethods
                .FirstOrDefaultAsync(p => p.UserId == userId && p.IsDefault && p.IsActive);
        }

        public async Task CreateAsync(PaymentMethod paymentMethod)
        {
            // If this is the first payment method, set it as default
            var userPaymentMethods = await GetByUserIdAsync(paymentMethod.UserId);
            if (!userPaymentMethods.Any())
            {
                paymentMethod.IsDefault = true;
            }

            // If setting as default, unset any existing default
            if (paymentMethod.IsDefault)
            {
                await UnsetDefaultPaymentMethodsAsync(paymentMethod.UserId);
            }

            await _context.PaymentMethods.AddAsync(paymentMethod);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(PaymentMethod paymentMethod)
        {
            // If setting as default, unset any existing default
            if (paymentMethod.IsDefault)
            {
                await UnsetDefaultPaymentMethodsAsync(paymentMethod.UserId);
            }

            _context.PaymentMethods.Update(paymentMethod);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> DeleteAsync(string id)
        {
            var paymentMethod = await GetByIdAsync(id);
            if (paymentMethod == null) return false;

            paymentMethod.IsActive = false;
            await _context.SaveChangesAsync();

            // If this was the default payment method, set a new default
            if (paymentMethod.IsDefault)
            {
                var newDefault = await _context.PaymentMethods
                    .Where(p => p.UserId == paymentMethod.UserId && p.IsActive)
                    .FirstOrDefaultAsync();

                if (newDefault != null)
                {
                    newDefault.IsDefault = true;
                    await _context.SaveChangesAsync();
                }
            }

            return true;
        }

        public async Task SetDefaultPaymentMethodAsync(string userId, string paymentMethodId)
        {
            await UnsetDefaultPaymentMethodsAsync(userId);

            var paymentMethod = await GetByIdAsync(paymentMethodId);
            if (paymentMethod != null && paymentMethod.UserId == userId)
            {
                paymentMethod.IsDefault = true;
                await _context.SaveChangesAsync();
            }
        }

        private async Task UnsetDefaultPaymentMethodsAsync(string userId)
        {
            var defaultPaymentMethods = await _context.PaymentMethods
                .Where(p => p.UserId == userId && p.IsDefault && p.IsActive)
                .ToListAsync();

            foreach (var pm in defaultPaymentMethods)
            {
                pm.IsDefault = false;
            }

            await _context.SaveChangesAsync();
        }
    }
}