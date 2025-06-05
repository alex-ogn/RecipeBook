using System.ComponentModel.DataAnnotations;

namespace RecipeBook.ViewModels.Account
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Моля въведете потребителско име или имейл.")]
        [Display(Name = "Потребителско име или имейл")]
        public string Identifier { get; set; }

        [Required(ErrorMessage = "Моля въведете парола.")]
        [DataType(DataType.Password)]
        [Display(Name = "Парола")]
        public string Password { get; set; }

        [Display(Name = "Запомни ме")]
        public bool RememberMe { get; set; }
    }
}
