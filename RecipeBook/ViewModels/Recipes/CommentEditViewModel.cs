using System.ComponentModel.DataAnnotations;

namespace RecipeBook.ViewModels.Recipes
{
    public class CommentEditViewModel
    {
        public int Id { get; set; }

        [Required]
        [StringLength(1000, MinimumLength = 2)]
        public string Content { get; set; }
    }
}
