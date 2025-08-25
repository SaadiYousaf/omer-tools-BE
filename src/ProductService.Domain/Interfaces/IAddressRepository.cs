// ProductService.Domain/Interfaces/IAddressRepository.cs
using ProductService.Domain.Entites;
using ProductService.Domain.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ProductService.Domain.Interfaces
{
    public interface IAddressRepository
    {
        Task<Address> GetByIdAsync(string id);
        Task<IEnumerable<Address>> GetByUserIdAsync(string userId);
        Task<Address> GetDefaultAddressAsync(string userId);
        Task CreateAsync(Address address);
        Task UpdateAsync(Address address);
        Task<bool> DeleteAsync(string id);
        Task SetDefaultAddressAsync(string userId, string addressId);
    }
}