using System.ComponentModel.DataAnnotations;
using RecipeBook.Resources;

namespace RecipeBook.ViewModels.Account;

public class ResetPasswordViewModel
{
    [Required(ErrorMessageResourceName = nameof(ValidationTexts.FieldRequired), ErrorMessageResourceType = typeof(ValidationTexts))]
    [EmailAddress(ErrorMessageResourceName = nameof(ValidationTexts.InvalidEmail), ErrorMessageResourceType = typeof(ValidationTexts))]
    [Display(Name = nameof(DisplayTexts.Email), ResourceType = typeof(DisplayTexts))]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Password reset token from the emailed link.
    /// </summary>
    [Required(ErrorMessageResourceName = nameof(ValidationTexts.FieldRequired), ErrorMessageResourceType = typeof(ValidationTexts))]
    public string Token { get; set; } = string.Empty;

    [Required(ErrorMessageResourceName = nameof(ValidationTexts.FieldRequired), ErrorMessageResourceType = typeof(ValidationTexts))]
    [DataType(DataType.Password)]
    [Display(Name = nameof(DisplayTexts.NewPassword), ResourceType = typeof(DisplayTexts))]
    public string Password { get; set; } = string.Empty;

    [DataType(DataType.Password)]
    [Display(Name = nameof(DisplayTexts.ConfirmNewPassword), ResourceType = typeof(DisplayTexts))]
    [Compare(nameof(Password), ErrorMessageResourceName = nameof(ValidationTexts.PasswordsDoNotMatch), ErrorMessageResourceType = typeof(ValidationTexts))]
    public string ConfirmPassword { get; set; } = string.Empty;
}
