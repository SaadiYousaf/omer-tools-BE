// ProductService.Business/Interfaces/IAddressService.cs
using ProductService.Business.DTOs;
using ProductService.Business.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ProductService.Business.Interfaces
{
    public interface IAddressService
    {
        Task<AddressDto> GetAddressByIdAsync(string addressId);
        Task<IEnumerable<AddressDto>> GetUserAddressesAsync(string userId);
        Task<AddressDto> CreateAddressAsync(string userId, CreateAddressDto addressDto);
        Task<AddressDto> UpdateAddressAsync(string userId, string addressId, UpdateAddressDto addressDto);
        Task<bool> DeleteAddressAsync(string userId, string addressId);
        Task SetDefaultAddressAsync(string userId, string addressId);
    }
}
