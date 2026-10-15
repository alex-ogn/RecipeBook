using System.ComponentModel.DataAnnotations;
using RecipeBook.Resources;

namespace RecipeBook.ViewModels.Recipes
{
    public class RecipeIngredientViewModel
    {
        public int IngredientId { get; set; }          // винаги ще е число
        [StringLength(100, MinimumLength = 1, ErrorMessageResourceName = nameof(ValidationTexts.IngredientNameLength), ErrorMessageResourceType = typeof(ValidationTexts))]
        public string Name { get; set; }               // показваме го в интерфейса
        [StringLength(100, MinimumLength = 1, ErrorMessageResourceName = nameof(ValidationTexts.IngredientQuantityLength), ErrorMessageResourceType = typeof(ValidationTexts))]
        public string QuantityNeeded { get; set; }     // количество (в текстов вид за по-голяма гъвкавост)
        public bool IsNew { get; set; }                // маркира дали е нова съставка
    }
}
