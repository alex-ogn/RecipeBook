using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace RecipeBook.Models
{
    public class Recipe
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Title { get; set; }

        public string Description { get; set; }

        public string Instructions { get; set; }

        public int Servings { get; set; }

        public TimeSpan CookingTime { get; set; }

        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public IdentityUser User { get; set; }

        public List<RecipeIngredient> RecipeIngredients { get; set; }
    }
}
