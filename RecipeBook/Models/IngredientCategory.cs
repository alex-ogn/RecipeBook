using System.ComponentModel.DataAnnotations;
using RecipeBook.Resources;

namespace RecipeBook.Models
{
    public class IngredientCategory
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Display(Name = nameof(DisplayTexts.Name), ResourceType = typeof(DisplayTexts))]
        [StringLength(100)]
        public string Name { get; set; }

        public ICollection<Ingredient> Ingredients { get; set; } = new List<Ingredient>();

    }
}
