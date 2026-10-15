using System.ComponentModel.DataAnnotations;
using RecipeBook.Resources;

namespace RecipeBook.ViewModels.Account
{
    public class RegisterViewModel
    {
        [Required(ErrorMessageResourceName = nameof(ValidationTexts.FieldRequired), ErrorMessageResourceType = typeof(ValidationTexts))]
        [EmailAddress(ErrorMessageResourceName = nameof(ValidationTexts.InvalidEmail), ErrorMessageResourceType = typeof(ValidationTexts))]
        [Display(Name = nameof(DisplayTexts.Email), ResourceType = typeof(DisplayTexts))]
        public string Email { get; set; }

        [Required(ErrorMessageResourceName = nameof(ValidationTexts.FieldRequired), ErrorMessageResourceType = typeof(ValidationTexts))]
        [DataType(DataType.Password)]
        [Display(Name = nameof(DisplayTexts.Password), ResourceType = typeof(DisplayTexts))]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = nameof(DisplayTexts.ConfirmPassword), ResourceType = typeof(DisplayTexts))]
        [Compare(nameof(Password), ErrorMessageResourceName = nameof(ValidationTexts.PasswordsDoNotMatch), ErrorMessageResourceType = typeof(ValidationTexts))]
        public string ConfirmPassword { get; set; }
    }

}
