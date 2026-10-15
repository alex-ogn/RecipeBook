using RecipeBook.Models;
using RecipeBook.ViewModels.Shared;

namespace RecipeBook.Services
{
    /// <summary>
    /// Id and name of an ingredient, used by the ingredient picker on the recipe forms.
    /// </summary>
    public record IngredientOption(int Id, string Name);

    /// <summary>
    /// CRUD and lookups for ingredients.
    /// </summary>
    public interface IIngredientService
    {
        /// <summary>
        /// One page of ingredients (with their category) whose name contains <paramref name="search"/>,
        /// optionally in one category, ordered by name.
        /// </summary>
        Task<PagedResult<Ingredient>> SearchAsync(string? search, int? categoryId, int page, int pageSize);
        Task<Ingredient?> GetByIdAsync(int id);
        Task<List<IngredientOption>> GetOptionsAsync();
        Task<bool> ExistsInCategoryAsync(string name, int categoryId, int? excludeId = null);
        Task CreateAsync(Ingredient ingredient);

        /// <returns>False when the ingredient no longer exists.</returns>
        Task<bool> UpdateAsync(Ingredient ingredient);

        /// <summary>
        /// Deletes the ingredient unless recipes still use it.
        /// </summary>
        Task<DeleteResult> DeleteAsync(int id);
    }
}
