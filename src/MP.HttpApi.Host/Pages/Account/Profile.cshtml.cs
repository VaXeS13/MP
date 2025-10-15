using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using System.Linq;
using Volo.Abp.Identity;
using Volo.Abp.Users;
using Microsoft.AspNetCore.Authorization;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MP.Domain.Identity;
using Microsoft.Extensions.DependencyInjection;
using MP.EntityFrameworkCore;

namespace MP.HttpApi.Host.Pages.Account
{
    [Authorize]
    public class ProfileModel : PageModel
    {
        private readonly ILogger<ProfileModel> _logger;
        private readonly IdentityUserManager _userManager;
        private readonly ICurrentUser _currentUser;
        private readonly IServiceProvider _serviceProvider;

        public ProfileModel(
            ILogger<ProfileModel> logger,
            IdentityUserManager userManager,
            ICurrentUser currentUser,
            IServiceProvider serviceProvider)
        {
            _logger = logger;
            _userManager = userManager;
            _currentUser = currentUser;
            _serviceProvider = serviceProvider;
        }

        [BindProperty(SupportsGet = true)]
        public ProfileInputModel Input { get; set; }

        [TempData]
        public string StatusMessage { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetByIdAsync(_currentUser.GetId());
            if (user == null)
            {
                return NotFound();
            }

            Input = new ProfileInputModel
            {
                Name = user.Name,
                Surname = user.Surname,
                Email = user.Email,
                // PhoneNumber is read-only in IdentityUser, we'll get it from UserProfile if needed
                BankAccountNumber = await GetBankAccountNumberFromUserAsync(user)
            };

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var user = await _userManager.GetByIdAsync(_currentUser.GetId());
            if (user == null)
            {
                return NotFound();
            }

            // Update basic properties
            user.Name = Input.Name;
            user.Surname = Input.Surname;

            // Note: PhoneNumber is read-only in IdentityUser, we'll need to use a different approach
            // user.PhoneNumber = Input.PhoneNumber;

            // Note: Email changes typically require email confirmation in ABP
            // For now, we'll skip email updates or handle them separately
            // user.Email = Input.Email;

            // Update BankAccountNumber
            await SetBankAccountNumberForUserAsync(user, Input.BankAccountNumber);

            var result = await _userManager.UpdateAsync(user);

            if (result.Succeeded)
            {
                StatusMessage = "Profil został zaktualizowany pomyślnie";
                _logger.LogInformation("User {UserId} updated their profile", user.Id);
                return RedirectToPage();
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return Page();
        }

        private async Task<string> GetBankAccountNumberFromUserAsync(IdentityUser user)
        {
            // Get the UserProfile from the database using service provider
            var dbContext = _serviceProvider.GetRequiredService<MPDbContext>();
            var userProfile = await dbContext.Set<UserProfile>()
                .FirstOrDefaultAsync(up => up.UserId == user.Id);

            return userProfile?.BankAccountNumber ?? string.Empty;
        }

        private async Task SetBankAccountNumberForUserAsync(IdentityUser user, string bankAccountNumber)
        {
            // Get the UserProfile from the database using service provider
            var dbContext = _serviceProvider.GetRequiredService<MPDbContext>();
            var userProfile = await dbContext.Set<UserProfile>()
                .FirstOrDefaultAsync(up => up.UserId == user.Id);

            if (userProfile == null)
            {
                // Create new UserProfile if it doesn't exist
                userProfile = new UserProfile
                {
                    UserId = user.Id,
                    BankAccountNumber = bankAccountNumber
                };
                await dbContext.Set<UserProfile>().AddAsync(userProfile);
            }
            else
            {
                // Update existing UserProfile
                userProfile.BankAccountNumber = bankAccountNumber;
                dbContext.Set<UserProfile>().Update(userProfile);
            }

            // Save changes to the database
            await dbContext.SaveChangesAsync();
        }
    }

    public class ProfileInputModel
    {
        [Required(ErrorMessage = "Imię jest wymagane")]
        [StringLength(50, ErrorMessage = "Imię nie może przekraczać 50 znaków")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Nazwisko jest wymagane")]
        [StringLength(50, ErrorMessage = "Nazwisko nie może przekraczać 50 znaków")]
        public string Surname { get; set; }

        [Required(ErrorMessage = "Email jest wymagany")]
        [EmailAddress(ErrorMessage = "Nieprawidłowy format adresu email")]
        public string Email { get; set; }

        
        [StringLength(50, ErrorMessage = "Numer konta bankowego nie może przekraczać 50 znaków")]
        [RegularExpression(@"^(PL)?\d{26}$|^[A-Z]{2}\d{2}[A-Z0-9]{1,30}$",
            ErrorMessage = "Nieprawidłowy format numeru konta bankowego (26 cyfr lub format IBAN)")]
        public string BankAccountNumber { get; set; }
    }
}