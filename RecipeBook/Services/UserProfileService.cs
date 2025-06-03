using Microsoft.AspNetCore.Identity;
using RecipeBook.Models;
using RecipeBook.ViewModels.Users;
using System.Text.RegularExpressions;

namespace RecipeBook.Services
{
    public class UserProfileService : IUserProfileService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IPasswordValidator<ApplicationUser> _passwordValidator;
        private readonly IPasswordHasher<ApplicationUser> _passwordHasher;

        public UserProfileService(
            UserManager<ApplicationUser> userManager,
            IPasswordValidator<ApplicationUser> passwordValidator,
            IPasswordHasher<ApplicationUser> passwordHasher)
        {
            _userManager = userManager;
            _passwordValidator = passwordValidator;
            _passwordHasher = passwordHasher;
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
    }

}
