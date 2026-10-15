using System.ComponentModel.DataAnnotations;
using RecipeBook.Resources;

namespace RecipeBook.ViewModels.Account
{
    public class LoginViewModel
    {
        [Required(ErrorMessageResourceName = nameof(ValidationTexts.LoginIdentifierRequired), ErrorMessageResourceType = typeof(ValidationTexts))]
        [Display(Name = nameof(DisplayTexts.UserNameOrEmail), ResourceType = typeof(DisplayTexts))]
        public string Identifier { get; set; } = string.Empty;

        [Required(ErrorMessageResourceName = nameof(ValidationTexts.PasswordRequired), ErrorMessageResourceType = typeof(ValidationTexts))]
        [DataType(DataType.Password)]
        [Display(Name = nameof(DisplayTexts.Password), ResourceType = typeof(DisplayTexts))]
        public string Password { get; set; } = string.Empty;

        [Display(Name = nameof(DisplayTexts.RememberMe), ResourceType = typeof(DisplayTexts))]
        public bool RememberMe { get; set; }
    }
}
