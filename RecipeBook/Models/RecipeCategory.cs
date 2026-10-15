
using System.ComponentModel.DataAnnotations;

namespace RecipeBook.Models
{
    public class RecipeCategory
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        public ICollection<Recipe> Recipes { get; set; } = new List<Recipe>();

    }
}
