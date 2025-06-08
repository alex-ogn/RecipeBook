using System.ComponentModel.DataAnnotations;

namespace RecipeBook.ViewModels.Recipes
{
    public class RecipeIngredientViewModel
    {
        public int IngredientId { get; set; }          // винаги ще е число
        [StringLength(100, MinimumLength = 1, ErrorMessage = "Имената на съставкатите трябва да са дълги между 1 и 100 символа.")]
        public string Name { get; set; }               // показваме го в интерфейса
        [StringLength(100, MinimumLength = 1, ErrorMessage = "Количествата на съставкатите трябва да са дълги между 1 и 100 символа.")]
        public string QuantityNeeded { get; set; }     // количество (в текстов вид за по-голяма гъвкавост)
        public bool IsNew { get; set; }                // маркира дали е нова съставка
    }
}
