using System.ComponentModel.DataAnnotations;
using System.Xml.Linq;

namespace RecipeBook.ViewModels.Account
{
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Полето {0} е задължително.")]
        [EmailAddress(ErrorMessage = "Полето {0} трябва да съдържа валиден имейл адрес.")]
        [Display(Name = "Имейл")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Полето {0} е задължително.")]
        [DataType(DataType.Password)]
        [Display(Name = "Парола")]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Потвърди паролата")]
        [Compare("Password", ErrorMessage = "Паролите не съвпадат.")]
        public string ConfirmPassword { get; set; }
    }

}
