using System.ComponentModel.DataAnnotations;
using RecipeBook.Constants;
using RecipeBook.Resources;

namespace RecipeBook.ViewModels.Users
{
    public class EditUserViewModel
    {
        public string Id { get; set; } = string.Empty; // за админски достъп

        [Required]
        [Display(Name = nameof(DisplayTexts.UserName), ResourceType = typeof(DisplayTexts))]
        public string UserName { get; set; } = string.Empty;

        // Shown read-only: the email is changed through Account/ChangeEmail, which confirms the new address.
        [Display(Name = nameof(DisplayTexts.Email), ResourceType = typeof(DisplayTexts))]
        public string Email { get; set; } = string.Empty;

        [Phone(ErrorMessageResourceName = nameof(ValidationTexts.InvalidPhoneNumber), ErrorMessageResourceType = typeof(ValidationTexts))]
        [Display(Name = nameof(DisplayTexts.PhoneNumber), ResourceType = typeof(DisplayTexts))]
        public string? PhoneNumber { get; set; }

        [Display(Name = nameof(DisplayTexts.ProfilePicture), ResourceType = typeof(DisplayTexts))]
        public IFormFile? ProfilePicture { get; set; }

        public string ExistingProfilePictureUrl { get; set; } = ImageConstants.DefaultProfilePictureUrl; // the stored picture or the default, for the views

        // Account state, shown on the form only (never read from the POST).
        public bool EmailConfirmed { get; set; }

        public bool IsLockedOut { get; set; }

        public bool IsDisabled { get; set; }

        public DateTimeOffset? LockoutEnd { get; set; }

        public bool IsOwnProfile { get; set; }

    }

}
