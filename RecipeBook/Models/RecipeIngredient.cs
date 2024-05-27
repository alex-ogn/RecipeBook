using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace RecipeBook.Models
{
    public class RecipeIngredient
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey("Recipe")]
        public int RecipeId { get; set; }

        public Recipe Recipe { get; set; }

        [Required]
        [ForeignKey("Ingredient")]
        public int IngredientId { get; set; }

        public Ingredient Ingredient { get; set; }

        public string QuantityNeeded { get; set; }
    }
}
