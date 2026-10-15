using System.ComponentModel.DataAnnotations;
using RecipeBook.Resources;

namespace RecipeBook.ViewModels.Users
{
    public class EditUserViewModel
    {
        public string Id { get; set; } // за админски достъп

        [Required]
        [Display(Name = nameof(DisplayTexts.UserName), ResourceType = typeof(DisplayTexts))]
        public string UserName { get; set; }

        [Required]
        [EmailAddress]
        [Display(Name = nameof(DisplayTexts.Email), ResourceType = typeof(DisplayTexts))]
        public string Email { get; set; }

        [Phone(ErrorMessageResourceName = nameof(ValidationTexts.InvalidPhoneNumber), ErrorMessageResourceType = typeof(ValidationTexts))]
        [Display(Name = nameof(DisplayTexts.PhoneNumber), ResourceType = typeof(DisplayTexts))]
        public string? PhoneNumber { get; set; }

        [Display(Name = nameof(DisplayTexts.NewPassword), ResourceType = typeof(DisplayTexts))]
        [DataType(DataType.Password)]
        public string? NewPassword { get; set; }

        [Display(Name = nameof(DisplayTexts.ConfirmNewPassword), ResourceType = typeof(DisplayTexts))]
        [DataType(DataType.Password)]
        [Compare(nameof(NewPassword), ErrorMessageResourceName = nameof(ValidationTexts.PasswordsDoNotMatch), ErrorMessageResourceType = typeof(ValidationTexts))]
        public string? ConfirmPassword { get; set; }

        [Display(Name = nameof(DisplayTexts.ProfilePicture), ResourceType = typeof(DisplayTexts))]
        public IFormFile? ProfilePicture { get; set; }

        public string? ExistingProfilePictureUrl { get; set; } // за показване в изгледите

        public int ProfilePictureVersion { get; set; } // за да се показва винаги най-новата

    }

}
