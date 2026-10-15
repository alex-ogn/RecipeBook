using RecipeBook.Models;

namespace RecipeBook.Services
{
    /// <summary>
    /// CRUD for recipe categories.
    /// </summary>
    public interface IRecipeCategoryService
    {
        Task<List<RecipeCategory>> GetAllAsync();
        Task<RecipeCategory?> GetByIdAsync(int id);
        /// <summary>
        /// First category whose name contains <paramref name="namePart"/> (case-insensitive), or null.
        /// </summary>
        Task<RecipeCategory?> FindByNamePartAsync(string namePart);
        Task<bool> NameExistsAsync(string name, int? excludeId = null);
        Task CreateAsync(RecipeCategory category);

        /// <returns>False when the category no longer exists.</returns>
        Task<bool> UpdateAsync(RecipeCategory category);

        /// <summary>
        /// Deletes the category unless recipes still use it.
        /// </summary>
        Task<DeleteResult> DeleteAsync(int id);
    }
}
