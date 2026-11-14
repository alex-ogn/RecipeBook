using System.ComponentModel.DataAnnotations;
using RecipeBook.Resources;

namespace RecipeBook.ViewModels.Account;

public class ChangePasswordViewModel
{
    [Required(ErrorMessageResourceName = nameof(ValidationTexts.FieldRequired), ErrorMessageResourceType = typeof(ValidationTexts))]
    [DataType(DataType.Password)]
    [Display(Name = nameof(DisplayTexts.CurrentPassword), ResourceType = typeof(DisplayTexts))]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required(ErrorMessageResourceName = nameof(ValidationTexts.FieldRequired), ErrorMessageResourceType = typeof(ValidationTexts))]
    [DataType(DataType.Password)]
    [Display(Name = nameof(DisplayTexts.NewPassword), ResourceType = typeof(DisplayTexts))]
    public string NewPassword { get; set; } = string.Empty;

    [DataType(DataType.Password)]
    [Display(Name = nameof(DisplayTexts.ConfirmNewPassword), ResourceType = typeof(DisplayTexts))]
    [Compare(nameof(NewPassword), ErrorMessageResourceName = nameof(ValidationTexts.PasswordsDoNotMatch), ErrorMessageResourceType = typeof(ValidationTexts))]
    public string ConfirmNewPassword { get; set; } = string.Empty;
}
