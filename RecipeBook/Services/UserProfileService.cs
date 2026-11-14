using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using RecipeBook.Constants;
using RecipeBook.Data;
using RecipeBook.Models;
using RecipeBook.Resources;
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
        private readonly IRecipeService _recipeService;
        private readonly IWebHostEnvironment _env;
        private readonly IImageStorageService _imageStorage;
        private readonly IStringLocalizer _localizer;

        public UserProfileService(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IRecipeService recipeService,
            IWebHostEnvironment env,
            IImageStorageService imageStorage,
            IStringLocalizerFactory factory)
        {
            _context = context;
            _userManager = userManager;
            _recipeService = recipeService;
            _env = env;
            _imageStorage = imageStorage;
            _localizer = factory.Create("IdentityErrorTexts", Assembly.GetExecutingAssembly().GetName().Name!);

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
            // Validate the picture before anything is saved, so a bad file never leaves a half-updated profile.
            if (model.ProfilePicture != null)
            {
                var imageError = _imageStorage.Validate(model.ProfilePicture);
                if (imageError != null)
                {
                    return IdentityResult.Failed(new IdentityError { Description = imageError });
                }
            }

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

            // Email and password are not changed here: they have their own forms in AccountController,
            // which ask for the current password and confirm the new email address.
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

            return await DeleteUserDataAsync(targetUserId);
        }

        /// <summary>
        /// Deletes the user's own account with all of their recipes. Admins can't delete themselves,
        /// so the site always keeps its admin account.
        /// </summary>
        public async Task<IdentityResult> DeleteOwnAccountAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return IdentityResult.Failed(new IdentityError { Description = _localizer["UserNotFound"] });

            if (await _userManager.IsInRoleAsync(user, UserRoles.Admin))
                return IdentityResult.Failed(new IdentityError { Description = MessageTexts.AdminCannotDeleteOwnAccount });

            return await DeleteUserDataAsync(userId);
        }

        private async Task<IdentityResult> DeleteUserDataAsync(string userId)
        {
            var user = await _userManager.Users
                .Include(u => u.Recipes)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
                return IdentityResult.Failed(new IdentityError { Description = _localizer["UserNotFound"] });

            // Deletes recipes
            foreach (var recipe in user.Recipes.ToList())
            {
                await _recipeService.DeleteRecipeAsync(recipe.Id, forceDelete: true);
            }

            // Delete related records
            _context.SavedRecipes.RemoveRange(_context.SavedRecipes.Where(x => x.UserId == userId));
            _context.RecipeLikes.RemoveRange(_context.RecipeLikes.Where(x => x.UserId == userId));
            _context.UserFollowers.RemoveRange(_context.UserFollowers.Where(f => f.FollowerId == userId || f.FollowedId == userId));
            _context.RecipeComments.RemoveRange(_context.RecipeComments.Where(c => c.UserId == userId));

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

            var defaultPath = Path.Combine(_env.WebRootPath, ImageConstants.ImagesFolder, UserProfileConstants.DefaultProfilePictureName);
            var defaultImage = await File.ReadAllBytesAsync(defaultPath);
            return (defaultImage, UserProfileConstants.DefaultProfilePictureFormat);
        }

    }

}
