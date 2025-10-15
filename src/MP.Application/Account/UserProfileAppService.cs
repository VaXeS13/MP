using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp.Application.Services;
using Volo.Abp.Users;
using Volo.Abp.Identity;
using Volo.Abp.Domain.Repositories;
using MP.Account;
using MP.Domain.Identity;

namespace MP.Account
{
    [Authorize]
    public class UserProfileAppService : ApplicationService, IUserProfileAppService
    {
        private readonly IdentityUserManager _userManager;
        private readonly IRepository<UserProfile, Guid> _userProfileRepository;

        public UserProfileAppService(
            IdentityUserManager userManager,
            IRepository<UserProfile, Guid> userProfileRepository)
        {
            _userManager = userManager;
            _userProfileRepository = userProfileRepository;
        }

        public async Task<UserProfileDto> GetAsync()
        {
            var currentUser = CurrentUser;
            if (currentUser == null)
            {
                throw new Volo.Abp.Authorization.AbpAuthorizationException("User not authenticated");
            }

            var user = await _userManager.GetByIdAsync(currentUser.GetId());
            if (user == null)
            {
                throw new Volo.Abp.BusinessException("User not found");
            }

            var userProfile = await _userProfileRepository.FirstOrDefaultAsync(up => up.UserId == user.Id);

            return new UserProfileDto
            {
                Name = user.Name,
                Surname = user.Surname,
                Email = user.Email,
                BankAccountNumber = userProfile?.BankAccountNumber
            };
        }

        public async Task<UserProfileDto> UpdateAsync(UserProfileDto input)
        {
            var currentUser = CurrentUser;
            if (currentUser == null)
            {
                throw new Volo.Abp.Authorization.AbpAuthorizationException("User not authenticated");
            }

            var user = await _userManager.GetByIdAsync(currentUser.GetId());
            if (user == null)
            {
                throw new Volo.Abp.BusinessException("User not found");
            }

            // Update basic user properties
            user.Name = input.Name;
            user.Surname = input.Surname;

            // Note: Email changes typically require email confirmation in ABP
            // For now, we'll skip email updates or handle them separately

            await _userManager.UpdateAsync(user);

            // Update bank account number in UserProfile
            var userProfile = await _userProfileRepository.FirstOrDefaultAsync(up => up.UserId == user.Id);

            if (userProfile == null)
            {
                // Create new UserProfile if it doesn't exist
                userProfile = new UserProfile
                {
                    UserId = user.Id,
                    BankAccountNumber = input.BankAccountNumber
                };
                await _userProfileRepository.InsertAsync(userProfile);
            }
            else
            {
                // Update existing UserProfile
                userProfile.BankAccountNumber = input.BankAccountNumber;
                await _userProfileRepository.UpdateAsync(userProfile);
            }

            return new UserProfileDto
            {
                Name = user.Name,
                Surname = user.Surname,
                Email = user.Email,
                BankAccountNumber = userProfile.BankAccountNumber
            };
        }
    }
}