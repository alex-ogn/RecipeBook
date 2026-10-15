using Microsoft.AspNetCore.Mvc.Rendering;
using RecipeBook.Models;
using RecipeBook.ViewModels.Enums;
using RecipeBook.ViewModels.Shared;

namespace RecipeBook.ViewModels.Recipes
{
    public class RecipeFilterViewModel
    {
        public PagedResult<RecipeCardViewModel> Recipes { get; set; } = new(Array.Empty<RecipeCardViewModel>(), 1, 1, 0);
        public SelectList Categories { get; set; }
        public int? SelectedCategoryId { get; set; }

        public RecipeSortOption SortOrder { get; set; } = RecipeSortOption.Newest;
    }
}
