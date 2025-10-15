using AngleSharp.Css.Values;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using RecipeBook.Constants;
using RecipeBook.Data;
using RecipeBook.Models;
using RecipeBook.ViewModels.Users;
using System.Reflection;
using System.Text.RegularExpressions;

namespace RecipeBook.Services
{
    /// <summary>
    /// Class for managing user profile
    /// </summary>
    public class UserProfileService : IUserProfileService
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IPasswordValidator<ApplicationUser> _passwordValidator;
        private readonly IPasswordHasher<ApplicationUser> _passwordHasher;
        private readonly IRecipeService _recipeService;
        private readonly IWebHostEnvironment _env;
        private readonly IStringLocalizer _localizer;

        public UserProfileService(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IPasswordValidator<ApplicationUser> passwordValidator,
            IPasswordHasher<ApplicationUser> passwordHasher,
            IRecipeService recipeService,
            IWebHostEnvironment env,
            IStringLocalizerFactory factory)
        {
            _context = context;
            _userManager = userManager;
            _passwordValidator = passwordValidator;
            _passwordHasher = passwordHasher;
            _recipeService = recipeService;
            _env = env;
            _localizer = factory.Create("IdentityErrorTexts", Assembly.GetExecutingAssembly().GetName().Name);

        }

        /// <summary>
        /// Updates user profile
        /// </summary>
        /// <param name="user"></param>
        /// <param name="model"></param>
        /// <param name="isAdmin"></param>
        /// <returns></returns>
        public async Task<IdentityResult> UpdateUserProfileAsync(ApplicationUser user, EditUserViewModel model, bool isAdmin = false)
        {
            if (user.UserName != model.UserName)
            {
                var existingUser = await _userManager.FindByNameAsync(model.UserName);
                if (existingUser != null && existingUser.Id != user.Id)
                {
                    var error = IdentityResult.Failed(new IdentityError { Description = _localizer["ExistingUserName"] });
                    return error;
                }

                if (model.UserName.Length < UserProfileConstants.MinUserNameLength)
                {
                    var error = IdentityResult.Failed(new IdentityError { Description = string.Format(_localizer["UserNameToShort"], UserProfileConstants.MinUserNameLength.ToString()) });
                    return error;
                }

                var setUserNameResult = await _userManager.SetUserNameAsync(user, model.UserName);
                if (!setUserNameResult.Succeeded) return setUserNameResult;
            }

            // Email
            if (user.Email != model.Email)
            {
                var setEmailResult = await _userManager.SetEmailAsync(user, model.Email);
                if (!setEmailResult.Succeeded) return setEmailResult;
            }

            // Phone number
            if (user.PhoneNumber != model.PhoneNumber)
            {
                var phoneRegex = new Regex(UserProfileConstants.PhoneNumberRegexPattern);
                if (!string.IsNullOrEmpty(model.PhoneNumber) && !phoneRegex.IsMatch(model.PhoneNumber))
                {
                    return IdentityResult.Failed(new IdentityError
                    {
                         Description = _localizer["InvalidPhoneNumberWithSuggestions"]
                    });
                }

                var setPhoneResult = await _userManager.SetPhoneNumberAsync(user, model.PhoneNumber);
                if (!setPhoneResult.Succeeded) return setPhoneResult;
            }

            // Profile picture
            if (model.ProfilePicture != null)
            {
                using var ms = new MemoryStream();
                await model.ProfilePicture.CopyToAsync(ms);
                user.ProfilePicture = ms.ToArray();
                user.ProfilePictureVersion++;
            }

            // Password
            if (!string.IsNullOrWhiteSpace(model.NewPassword))
            {
                var passwordValidation = await _passwordValidator.ValidateAsync(_userManager, user, model.NewPassword);
                if (!passwordValidation.Succeeded) return passwordValidation;

                user.PasswordHash = _passwordHasher.HashPassword(user, model.NewPassword);
            }

            return await _userManager.UpdateAsync(user);
        }

        /// <summary>
        /// Deletes user profile
        /// </summary>
        /// <param name="targetUserId"></param>
        /// <param name="currentUserId"></param>
        /// <returns></returns>
        public async Task<IdentityResult> DeleteUserAsync(string targetUserId, string currentUserId)
        {         
            if (targetUserId == currentUserId)
                return IdentityResult.Failed(new IdentityError { Description = _localizer["CanNotDeleteYourOwnProfile"] });

            var user = await _userManager.Users
                .Include(u => u.Recipes)
                .FirstOrDefaultAsync(u => u.Id == targetUserId);

            if (user == null)
                return IdentityResult.Failed(new IdentityError { Description = _localizer["UserNotFound"] });

            // Deletes recipes
            foreach (var recipe in user.Recipes.ToList())
            {
                await _recipeService.DeleteRecipeAsync(recipe.Id, forceDelete: true);
            }

            // Delete related records
            _context.SavedRecipes.RemoveRange(_context.SavedRecipes.Where(x => x.UserId == targetUserId));
            _context.RecipeLikes.RemoveRange(_context.RecipeLikes.Where(x => x.UserId == targetUserId));
            _context.UserFollowers.RemoveRange(_context.UserFollowers.Where(f => f.FollowerId == targetUserId || f.FollowedId == targetUserId));
            _context.RecipeComments.RemoveRange(_context.RecipeComments.Where(c => c.UserId == targetUserId));

            await _context.SaveChangesAsync();

            return await _userManager.DeleteAsync(user);
        }

        /// <summary>
        /// Load profile picture, if not available - the default picture
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        public async Task<(byte[] Content, string ContentType)> GetProfilePictureAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);

            if (user != null && user.ProfilePicture != null)
            {
                return (user.ProfilePicture, UserProfileConstants.DefaultProfilePictureFormat);
            }

            var defaultPath = Path.Combine(_env.WebRootPath, "images", UserProfileConstants.DefaultProfilePictureName);
            var defaultImage = await File.ReadAllBytesAsync(defaultPath);
            return (defaultImage, UserProfileConstants.DefaultProfilePictureFormat);
        }

    }

}
