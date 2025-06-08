using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RecipeBook.Data;
using RecipeBook.Models;
using RecipeBook.ViewModels.Users;
using System.Text.RegularExpressions;

namespace RecipeBook.Services
{
    public class UserProfileService : IUserProfileService
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IPasswordValidator<ApplicationUser> _passwordValidator;
        private readonly IPasswordHasher<ApplicationUser> _passwordHasher;
        private readonly IRecipeService _recipeService;
        private readonly IWebHostEnvironment _env;

        public UserProfileService(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IPasswordValidator<ApplicationUser> passwordValidator,
            IPasswordHasher<ApplicationUser> passwordHasher,
            IRecipeService recipeService,
            IWebHostEnvironment env)
        {
            _context = context;
            _userManager = userManager;
            _passwordValidator = passwordValidator;
            _passwordHasher = passwordHasher;
            _recipeService = recipeService;
            _env = env;
        }

        public async Task<IdentityResult> UpdateUserProfileAsync(ApplicationUser user, EditUserViewModel model, bool isAdmin = false)
        {
            // Проверка за ново потребителско име
            if (user.UserName != model.UserName)
            {
                var existingUser = await _userManager.FindByNameAsync(model.UserName);
                if (existingUser != null && existingUser.Id != user.Id)
                {
                    var error = IdentityResult.Failed(new IdentityError { Description = "Потребителското име вече съществува." });
                    return error;
                }

                if (model.UserName.Length < 2)
                {
                    var error = IdentityResult.Failed(new IdentityError { Description = "Потребителското име трябва да има поне два символа." });
                    return error;
                }

                var setUserNameResult = await _userManager.SetUserNameAsync(user, model.UserName);
                if (!setUserNameResult.Succeeded) return setUserNameResult;
            }

            // Имейл
            if (user.Email != model.Email)
            {
                var setEmailResult = await _userManager.SetEmailAsync(user, model.Email);
                if (!setEmailResult.Succeeded) return setEmailResult;
            }

            // Телефон
            if (user.PhoneNumber != model.PhoneNumber)
            {
                var phoneRegex = new Regex(@"^(\+359|0)?8[7-9][0-9]{7}$");
                if (!string.IsNullOrEmpty(model.PhoneNumber) && !phoneRegex.IsMatch(model.PhoneNumber))
                {
                    return IdentityResult.Failed(new IdentityError
                    {
                        Description = "Телефонният номер е невалиден. Моля, използвайте формат като 0888123456 или +359888123456."
                    });
                }

                var setPhoneResult = await _userManager.SetPhoneNumberAsync(user, model.PhoneNumber);
                if (!setPhoneResult.Succeeded) return setPhoneResult;
            }

            // Профилна снимка
            if (model.ProfilePicture != null)
            {
                using var ms = new MemoryStream();
                await model.ProfilePicture.CopyToAsync(ms);
                user.ProfilePicture = ms.ToArray();
                user.ProfilePictureVersion++;
            }

            // Парола
            if (!string.IsNullOrWhiteSpace(model.NewPassword))
            {
                var passwordValidation = await _passwordValidator.ValidateAsync(_userManager, user, model.NewPassword);
                if (!passwordValidation.Succeeded) return passwordValidation;

                user.PasswordHash = _passwordHasher.HashPassword(user, model.NewPassword);
            }

            return await _userManager.UpdateAsync(user);
        }

        public async Task<IdentityResult> DeleteUserAsync(string targetUserId, string currentUserId)
        {
            if (targetUserId == currentUserId)
                return IdentityResult.Failed(new IdentityError { Description = "Не може да изтриете себе си." });

            var user = await _userManager.Users
                .Include(u => u.Recipes)
                .FirstOrDefaultAsync(u => u.Id == targetUserId);

            if (user == null)
                return IdentityResult.Failed(new IdentityError { Description = "Потребителят не е намерен." });

            // Изтриване на рецептите и свързани записи
            foreach (var recipe in user.Recipes.ToList())
            {
                await _recipeService.DeleteRecipeAsync(recipe.Id, forceDelete: true);
            }

            // Изтриване на други релации
            _context.SavedRecipes.RemoveRange(_context.SavedRecipes.Where(x => x.UserId == targetUserId));
            _context.RecipeLikes.RemoveRange(_context.RecipeLikes.Where(x => x.UserId == targetUserId));
            _context.UserFollowers.RemoveRange(_context.UserFollowers.Where(f => f.FollowerId == targetUserId || f.FollowedId == targetUserId));
            _context.RecipeComments.RemoveRange(_context.RecipeComments.Where(c => c.UserId == targetUserId));

            await _context.SaveChangesAsync();

            return await _userManager.DeleteAsync(user);
        }

        public async Task<(byte[] Content, string ContentType)> GetProfilePictureAsync(string userId)
        {
            var user = await _userManager.Users.FirstOrDefaultAsync(u => u.Id == userId);

            if (user != null && user.ProfilePicture != null)
            {
                return (user.ProfilePicture, "image/jpg");
            }

            // зареждаме default изображение от файлова система
            var defaultPath = Path.Combine(_env.WebRootPath, "images", "default-profile.png");
            var defaultImage = await File.ReadAllBytesAsync(defaultPath);
            return (defaultImage, "image/png");
        }

    }

}
