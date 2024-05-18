using System.ComponentModel.DataAnnotations;

namespace RecipeBook.Models
{
    public class IngredientCategory
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Name { get; set; }

    }
}
