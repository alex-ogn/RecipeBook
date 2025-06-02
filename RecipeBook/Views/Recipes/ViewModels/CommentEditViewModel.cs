using System.ComponentModel.DataAnnotations;

namespace RecipeBook.Views.Recipes.ViewModels
{
    public class CommentEditViewModel
    {
        public int Id { get; set; }

        [Required]
        [StringLength(1000, MinimumLength = 2)]
        public string Content { get; set; }
    }
}
