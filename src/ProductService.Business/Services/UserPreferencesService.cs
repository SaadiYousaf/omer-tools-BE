
// ProductService.Business/Services/UserPreferencesService.cs
using AutoMapper;
using ProductService.Business.DTOs;
using ProductService.Business.Interfaces;
using ProductService.Domain.Entites;
using ProductService.Domain.Entities;
using ProductService.Domain.Interfaces;
using System.Threading.Tasks;

namespace ProductService.Business.Services
{
    public class UserPreferencesService : IUserPreferencesService
    {

        private readonly IUserPreferencesRepository _userPreferencesRepository;
        private readonly IMapper _mapper;

        public UserPreferencesService(IUserPreferencesRepository userPreferencesRepository, IMapper mapper)
        {
            _userPreferencesRepository = userPreferencesRepository;
            _mapper = mapper;
        }

        public async Task<UserPreferencesDto> GetUserPreferencesAsync(string userId)
        {
            var preferences = await _userPreferencesRepository.GetByUserIdAsync(userId);
            return _mapper.Map<UserPreferencesDto>(preferences);
        }

        public async Task<UserPreferencesDto> UpdateUserPreferencesAsync(string userId, UpdateUserPreferencesDto preferencesDto)
        {
            var preferences = await _userPreferencesRepository.GetByUserIdAsync(userId);

            if (preferences == null)
            {
                // Create new preferences if they don't exist
                preferences = _mapper.Map<UserPreferences>(preferencesDto);
                preferences.UserId = userId;
                await _userPreferencesRepository.CreateAsync(preferences);
            }
            else
            {
                // Update existing preferences
                _mapper.Map(preferencesDto, preferences);
                await _userPreferencesRepository.UpdateAsync(preferences);
            }

            return _mapper.Map<UserPreferencesDto>(preferences);
        }
    }
}