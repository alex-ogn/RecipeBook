using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RecipeBook.Models
{
    public class SavedRecipe
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [ForeignKey("ApplicationUser")]
        public string UserId { get; set; } = string.Empty;

        public ApplicationUser User { get; set; } = null!;

        [Required]
        [ForeignKey("Recipe")]
        public int RecipeId { get; set; }

        public Recipe Recipe { get; set; } = null!;

        [Required]
        public DateTime DateSaved { get; set; } = DateTime.UtcNow;
    }
}