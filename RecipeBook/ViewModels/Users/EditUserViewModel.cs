using System.ComponentModel.DataAnnotations;
using System.Xml.Linq;

namespace RecipeBook.ViewModels.Users
{
    public class EditUserViewModel
    {
        public string Id { get; set; } // за админски достъп

        [Required]
        [Display(Name = "Потребителско име")]
        public string UserName { get; set; }

        [Required]
        [EmailAddress]
        [Display(Name = "Имейл")]
        public string Email { get; set; }

        [Phone(ErrorMessage = "Моля, въведете валиден телефонен номер.")]
        [Display(Name = "Телефон")]
        public string? PhoneNumber { get; set; }

        [Display(Name = "Нова парола")]
        [DataType(DataType.Password)]
        public string? NewPassword { get; set; }

        [Display(Name = "Потвърди парола")]
        [DataType(DataType.Password)]
        [Compare("NewPassword", ErrorMessage = "Паролите не съвпадат.")]
        public string? ConfirmPassword { get; set; }

        [Display(Name = "Профилна снимка")]
        public IFormFile? ProfilePicture { get; set; }

        public string? ExistingProfilePictureUrl { get; set; } // за показване в изгледите

        public int ProfilePictureVersion { get; set; } // за да се показва винаги най-новата

    }

}
