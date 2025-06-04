using Microsoft.EntityFrameworkCore;
using RecipeBook.Data;
using RecipeBook.Models;

namespace RecipeBook.Services
{
    public class RecipeService : IRecipeService
    {
        private readonly ApplicationDbContext _context;

        public RecipeService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<bool> DeleteRecipeAsync(int recipeId, string? currentUserId = null, bool forceDelete = false)
        {
            var recipe = await _context.Recipies
                .Include(r => r.Comments)
                .Include(r => r.Likes)
                .Include(r => r.SavedByUsers)
                .Include(r => r.RecipeIngredients)
                .FirstOrDefaultAsync(r => r.Id == recipeId);

            if (recipe == null) return false;

            // Ако не е админ и не е собственик
            if (!forceDelete && recipe.UserId != currentUserId)
                return false;

            _context.RecipeComments.RemoveRange(recipe.Comments);
            _context.RecipeLikes.RemoveRange(recipe.Likes);
            _context.SavedRecipes.RemoveRange(recipe.SavedByUsers);
            _context.RecipeIngredients.RemoveRange(recipe.RecipeIngredients);

            _context.Recipies.Remove(recipe);
            await _context.SaveChangesAsync();

            return true;
        }
    }
}
