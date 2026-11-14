using System.ComponentModel.DataAnnotations;
using RecipeBook.Resources;

namespace RecipeBook.ViewModels.Account;

public class ChangeEmailViewModel
{
    /// <summary>
    /// Shown on the form; never read from the POST.
    /// </summary>
    public string CurrentEmail { get; set; } = string.Empty;

    [Required(ErrorMessageResourceName = nameof(ValidationTexts.FieldRequired), ErrorMessageResourceType = typeof(ValidationTexts))]
    [EmailAddress(ErrorMessageResourceName = nameof(ValidationTexts.InvalidEmail), ErrorMessageResourceType = typeof(ValidationTexts))]
    [Display(Name = nameof(DisplayTexts.NewEmail), ResourceType = typeof(DisplayTexts))]
    public string NewEmail { get; set; } = string.Empty;

    [Required(ErrorMessageResourceName = nameof(ValidationTexts.FieldRequired), ErrorMessageResourceType = typeof(ValidationTexts))]
    [DataType(DataType.Password)]
    [Display(Name = nameof(DisplayTexts.CurrentPassword), ResourceType = typeof(DisplayTexts))]
    public string CurrentPassword { get; set; } = string.Empty;
}
