using Microsoft.AspNetCore.Identity;

namespace RecipeBook.Services
{
    /// <summary>
    /// Custom class for identity error messages
    /// </summary>
    public class CustomIdentityErrorDescriber : IdentityErrorDescriber
    {
        public override IdentityError PasswordTooShort(int length)
            => new IdentityError
            {
                Code = nameof(PasswordTooShort),
                Description = $"Паролата трябва да бъде поне {length} символа."
            };

        public override IdentityError PasswordRequiresNonAlphanumeric()
            => new IdentityError
            {
                Code = nameof(PasswordRequiresNonAlphanumeric),
                Description = "Паролата трябва да съдържа поне един специален символ (напр. !, @, #, и т.н.)."
            };

        public override IdentityError PasswordRequiresDigit()
            => new IdentityError
            {
                Code = nameof(PasswordRequiresDigit),
                Description = "Паролата трябва да съдържа поне една цифра."
            };

        public override IdentityError PasswordRequiresUpper()
            => new IdentityError
            {
                Code = nameof(PasswordRequiresUpper),
                Description = "Паролата трябва да съдържа поне една главна буква."
            };

        public override IdentityError PasswordRequiresLower()
            => new IdentityError
            {
                Code = nameof(PasswordRequiresLower),
                Description = "Паролата трябва да съдържа поне една малка буква."
            };
    }
}
