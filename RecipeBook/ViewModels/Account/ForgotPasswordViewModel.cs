using System.ComponentModel.DataAnnotations;
using RecipeBook.Resources;

namespace RecipeBook.ViewModels.Account;

public class ForgotPasswordViewModel
{
    [Required(ErrorMessageResourceName = nameof(ValidationTexts.FieldRequired), ErrorMessageResourceType = typeof(ValidationTexts))]
    [EmailAddress(ErrorMessageResourceName = nameof(ValidationTexts.InvalidEmail), ErrorMessageResourceType = typeof(ValidationTexts))]
    [Display(Name = nameof(DisplayTexts.Email), ResourceType = typeof(DisplayTexts))]
    public string Email { get; set; } = string.Empty;
}
