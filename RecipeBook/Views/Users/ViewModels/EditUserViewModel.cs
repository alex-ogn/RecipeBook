using System.ComponentModel.DataAnnotations;
using System.Xml.Linq;

namespace RecipeBook.Views.Users.ViewModels
{
    public class EditUserViewModel
    {
        public string Id { get; set; }

        [Required]
        [Display(Name = "Потребителско име")]
        public string UserName { get; set; }

        [Display(Name = "Имейл")]
        [EmailAddress]
        public string Email { get; set; }

        [Display(Name = "Нова парола")]
        [DataType(DataType.Password)]
        public string? NewPassword { get; set; }

        [Display(Name = "Профилна снимка")]
        public IFormFile? ProfilePicture { get; set; }
    }

}
