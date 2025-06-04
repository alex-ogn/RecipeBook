namespace RecipeBook.Services
{
    public interface IRecipeService
    {
        Task<bool> DeleteRecipeAsync(int recipeId, string? currentUserId = null, bool forceDelete = false);
    }

}
