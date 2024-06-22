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

        public int IngredientCategoryId { get; set; }

        [ForeignKey("IngredientCategoryId")]
        [Display(Name = "Category")]
        public IngredientCategory? Category { get; set; }

        public List<RecipeIngredient>? RecipeIngredients { get; set; }
    }
}
