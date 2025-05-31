using Microsoft.AspNetCore.Mvc.Rendering;
using RecipeBook.Models;
using RecipeBook.Views.Enums;

namespace RecipeBook.Views.Recipes.ViewModels
{
    public class RecipeFilterViewModel
    {
        public IEnumerable<RecipeCardViewModel> Recipes { get; set; }
        public SelectList Categories { get; set; }
        public int? SelectedCategoryId { get; set; }

        public RecipeSortOption SortOrder { get; set; } = RecipeSortOption.Newest;
    }
}
