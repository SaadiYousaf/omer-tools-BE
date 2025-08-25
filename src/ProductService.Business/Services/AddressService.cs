
// ProductService.Business/Services/AddressService.cs
using AutoMapper;
using ProductService.Business.DTOs;
using ProductService.Business.Interfaces;
using ProductService.DataAccess.Repositories;
using ProductService.Domain.Entites;
using ProductService.Domain.Entities;
using ProductService.Domain.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ProductService.Business.Services
{
    public class AddressService : IAddressService
    {
        private readonly IAddressRepository _addressRepository;
        private readonly IMapper _mapper;

        public AddressService(IAddressRepository addressRepository, IMapper mapper)
        {
            _addressRepository = addressRepository;
            _mapper = mapper;
        }

        public async Task<AddressDto> GetAddressByIdAsync(string addressId)
        {
            var address = await _addressRepository.GetByIdAsync(addressId);
            return _mapper.Map<AddressDto>(address);
        }

        public async Task<IEnumerable<AddressDto>> GetUserAddressesAsync(string userId)
        {
            var addresses = await _addressRepository.GetByUserIdAsync(userId);
            return _mapper.Map<IEnumerable<AddressDto>>(addresses);
        }

        public async Task<AddressDto> CreateAddressAsync(string userId, CreateAddressDto addressDto)
        {
            var address = _mapper.Map<Address>(addressDto);
            address.UserId = userId;

            await _addressRepository.CreateAsync(address);
            return _mapper.Map<AddressDto>(address);
        }

        public async Task<AddressDto> UpdateAddressAsync(string userId, string addressId, UpdateAddressDto addressDto)
        {
            var address = await _addressRepository.GetByIdAsync(addressId);
            if (address == null || address.UserId != userId)
                return null;

            _mapper.Map(addressDto, address);
            await _addressRepository.UpdateAsync(address);

            return _mapper.Map<AddressDto>(address);
        }

        public async Task<bool> DeleteAddressAsync(string userId, string addressId)
        {
            var address = await _addressRepository.GetByIdAsync(addressId);
            if (address == null || address.UserId != userId)
                return false;

            return await _addressRepository.DeleteAsync(addressId);
        }

        public async Task SetDefaultAddressAsync(string userId, string addressId)
        {
            await _addressRepository.SetDefaultAddressAsync(userId, addressId);
        }
    }
}