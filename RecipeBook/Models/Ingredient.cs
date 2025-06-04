using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace RecipeBook.Models
{
    public class Ingredient
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Name { get; set; }

        [Required]
        public int IngredientCategoryId { get; set; }

        [ForeignKey("IngredientCategoryId")]
        [Display(Name = "Category")]
        public IngredientCategory IngredientCategory { get; set; }

        public List<RecipeIngredient>? RecipeIngredients { get; set; }
    }
}
