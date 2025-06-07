using System.ComponentModel.DataAnnotations;

namespace RecipeBook.Models
{
    public class IngredientCategory
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Display(Name = "Име")]
        [StringLength(100)]
        public string Name { get; set; }

        public ICollection<Ingredient> Ingredients { get; set; } = new List<Ingredient>();

    }
}
