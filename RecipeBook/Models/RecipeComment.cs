using System.ComponentModel.DataAnnotations;

namespace RecipeBook.Models
{
    public class RecipeComment
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Content { get; set; } = string.Empty;

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Required]
        public string UserId { get; set; } = string.Empty;

        public ApplicationUser User { get; set; } = null!;

        [Required]
        public int RecipeId { get; set; }

        public Recipe Recipe { get; set; } = null!;
    }
}
