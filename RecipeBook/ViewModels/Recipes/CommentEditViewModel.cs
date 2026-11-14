using System.ComponentModel.DataAnnotations;
using RecipeBook.Resources;

namespace RecipeBook.ViewModels.Recipes
{
    public class CommentEditViewModel
    {
        private const int MaxContentLength = 1000;
        private const int MinContentLength = 2;

        public int Id { get; set; }

        // The recipe the comment belongs to, so the page can link back to it.
        // Display only: the controller always takes it from the database, never from the form.
        public int RecipeId { get; set; }

        [Display(Name = nameof(DisplayTexts.Comment), ResourceType = typeof(DisplayTexts))]
        [Required(ErrorMessageResourceName = nameof(ValidationTexts.FieldRequired), ErrorMessageResourceType = typeof(ValidationTexts))]
        [StringLength(MaxContentLength, MinimumLength = MinContentLength,
            ErrorMessageResourceName = nameof(ValidationTexts.CommentLength), ErrorMessageResourceType = typeof(ValidationTexts))]
        public string Content { get; set; } = string.Empty;
    }
}
