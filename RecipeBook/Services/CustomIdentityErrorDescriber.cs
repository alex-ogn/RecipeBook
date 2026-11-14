using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Localization;
using RecipeBook.Resources;
using System.Reflection;

namespace RecipeBook.Services
{
    /// <summary>
    /// Custom class for identity error messages
    /// </summary>
    public class CustomIdentityErrorDescriber : IdentityErrorDescriber
    {
        private readonly IStringLocalizer _localizer;

        public CustomIdentityErrorDescriber(IStringLocalizerFactory factory)
        {
            _localizer = factory.Create("IdentityErrorTexts", Assembly.GetExecutingAssembly().GetName().Name!);
        }

        public override IdentityError PasswordTooShort(int length)
            => new IdentityError
            {
                Code = nameof(PasswordTooShort),
                Description = string.Format(_localizer["PasswordTooShort"], length.ToString())
            };

        public override IdentityError PasswordRequiresNonAlphanumeric()
            => new IdentityError
            {
                Code = nameof(PasswordRequiresNonAlphanumeric),
                Description = _localizer["PasswordRequiresNonAlphanumeric"]

            };

        public override IdentityError PasswordRequiresDigit()
            => new IdentityError
            {
                Code = nameof(PasswordRequiresDigit),
                Description = _localizer["PasswordRequiresDigit"]
            };

        public override IdentityError PasswordRequiresUpper()
            => new IdentityError
            {
                Code = nameof(PasswordRequiresUpper),
                Description = _localizer["PasswordRequiresUpper"]
            };

        public override IdentityError PasswordRequiresLower()
            => new IdentityError
            {
                Code = nameof(PasswordRequiresLower),
                Description = _localizer["PasswordRequiresLower"]
            };

        public override IdentityError PasswordMismatch()
            => new IdentityError
            {
                Code = nameof(PasswordMismatch),
                Description = _localizer["PasswordMismatch"]
            };

        public override IdentityError InvalidToken()
            => new IdentityError
            {
                Code = nameof(InvalidToken),
                Description = _localizer["InvalidToken"]
            };

        public override IdentityError DuplicateEmail(string email)
            => new IdentityError
            {
                Code = nameof(DuplicateEmail),
                Description = _localizer["DuplicateEmail"]
            };
    }
}
