using Microsoft.AspNetCore.Mvc.Rendering;
using RecipeBook.Models;
using RecipeBook.ViewModels.Enums;
using RecipeBook.ViewModels.Shared;

namespace RecipeBook.ViewModels.Recipes
{
    public class RecipeFilterViewModel
    {
        public PagedResult<RecipeCardViewModel> Recipes { get; set; } = new(Array.Empty<RecipeCardViewModel>(), 1, 1, 0);
        public required SelectList Categories { get; set; }
        public int? SelectedCategoryId { get; set; }

        public RecipeSortOption SortOrder { get; set; } = RecipeSortOption.Newest;

        // Quick filters from the home page (time and diet). Their names are also the query-string keys.
        public int? MaxCookingTime { get; set; }
        public bool IsVegetarian { get; set; }
        public bool IsVegan { get; set; }
        public bool IsGlutenFree { get; set; }
        public bool IsLactoseFree { get; set; }

        public bool HasQuickFilter => MaxCookingTime.HasValue || IsVegetarian || IsVegan || IsGlutenFree || IsLactoseFree;
    }
}
