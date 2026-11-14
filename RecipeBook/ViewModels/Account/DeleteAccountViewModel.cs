using System.ComponentModel.DataAnnotations;
using RecipeBook.Resources;

namespace RecipeBook.ViewModels.Account;

public class DeleteAccountViewModel
{
    [Required(ErrorMessageResourceName = nameof(ValidationTexts.FieldRequired), ErrorMessageResourceType = typeof(ValidationTexts))]
    [DataType(DataType.Password)]
    [Display(Name = nameof(DisplayTexts.CurrentPassword), ResourceType = typeof(DisplayTexts))]
    public string CurrentPassword { get; set; } = string.Empty;

    [Range(typeof(bool), "true", "true", ErrorMessageResourceName = nameof(ValidationTexts.ConfirmationRequired), ErrorMessageResourceType = typeof(ValidationTexts))]
    [Display(Name = nameof(DisplayTexts.ConfirmDeleteAccount), ResourceType = typeof(DisplayTexts))]
    public bool Confirm { get; set; }
}
