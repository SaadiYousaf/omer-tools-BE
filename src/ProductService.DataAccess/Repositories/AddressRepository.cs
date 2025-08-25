// ProductService.DataAccess/Repositories/AddressRepository.cs
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
    public class AddressRepository : IAddressRepository
    {
        private readonly ProductDbContext _context;

        public AddressRepository(ProductDbContext context)
        {
            _context = context;
        }

        public async Task<Address> GetByIdAsync(string id)
        {
            return await _context.Addresses
                .FirstOrDefaultAsync(a => a.Id == id && a.IsActive);
        }

        public async Task<IEnumerable<Address>> GetByUserIdAsync(string userId)
        {
            return await _context.Addresses
                .Where(a => a.UserId == userId && a.IsActive)
                .OrderByDescending(a => a.IsDefault)
                .ThenBy(a => a.AddressType)
                .ToListAsync();
        }

        public async Task<Address> GetDefaultAddressAsync(string userId)
        {
            return await _context.Addresses
                .FirstOrDefaultAsync(a => a.UserId == userId && a.IsDefault && a.IsActive);
        }

        public async Task CreateAsync(Address address)
        {
            // If this is the first address, set it as default
            var userAddresses = await GetByUserIdAsync(address.UserId);
            if (!userAddresses.Any())
            {
                address.IsDefault = true;
            }

            // If setting as default, unset any existing default
            if (address.IsDefault)
            {
                await UnsetDefaultAddressesAsync(address.UserId);
            }

            await _context.Addresses.AddAsync(address);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Address address)
        {
            // If setting as default, unset any existing default
            if (address.IsDefault)
            {
                await UnsetDefaultAddressesAsync(address.UserId);
            }

            _context.Addresses.Update(address);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> DeleteAsync(string id)
        {
            var address = await GetByIdAsync(id);
            if (address == null) return false;

            address.IsActive = false;
            await _context.SaveChangesAsync();

            // If this was the default address, set a new default
            if (address.IsDefault)
            {
                var newDefault = await _context.Addresses
                    .Where(a => a.UserId == address.UserId && a.IsActive)
                    .FirstOrDefaultAsync();

                if (newDefault != null)
                {
                    newDefault.IsDefault = true;
                    await _context.SaveChangesAsync();
                }
            }

            return true;
        }

        public async Task SetDefaultAddressAsync(string userId, string addressId)
        {
            await UnsetDefaultAddressesAsync(userId);

            var address = await GetByIdAsync(addressId);
            if (address != null && address.UserId == userId)
            {
                address.IsDefault = true;
                await _context.SaveChangesAsync();
            }
        }

        private async Task UnsetDefaultAddressesAsync(string userId)
        {
            var defaultAddresses = await _context.Addresses
                .Where(a => a.UserId == userId && a.IsDefault && a.IsActive)
                .ToListAsync();

            foreach (var addr in defaultAddresses)
            {
                addr.IsDefault = false;
            }

            await _context.SaveChangesAsync();
        }
    }
}