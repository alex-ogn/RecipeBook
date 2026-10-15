using RecipeBook.Models;

namespace RecipeBook.Services
{
    /// <summary>
    /// CRUD for ingredient categories.
    /// </summary>
    public interface IIngredientCategoryService
    {
        Task<List<IngredientCategory>> GetAllAsync();
        Task<IngredientCategory?> GetByIdAsync(int id);
        Task<bool> NameExistsAsync(string name, int? excludeId = null);
        Task CreateAsync(IngredientCategory category);

        /// <returns>False when the category no longer exists.</returns>
        Task<bool> UpdateAsync(IngredientCategory category);

        /// <summary>
        /// Deletes the category unless ingredients still use it.
        /// </summary>
        Task<DeleteResult> DeleteAsync(int id);
    }
}
