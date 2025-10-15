using Humanizer;
using Microsoft.EntityFrameworkCore;
using RecipeBook.Data;
using RecipeBook.Models;
using RecipeBook.ViewModels.Recipes;
using System.Text.RegularExpressions;

namespace RecipeBook.Services
{
    /// <summary>
    /// Class for managing recipes
    /// </summary>
    public class RecipeService : IRecipeService
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public RecipeService(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        /// <summary>
        /// Delete recipes
        /// </summary>
        /// <param name="recipeId"></param>
        /// <param name="currentUserId"></param>
        /// <param name="forceDelete"></param>
        /// <returns></returns>
        public async Task<bool> DeleteRecipeAsync(int recipeId, string? currentUserId = null, bool forceDelete = false)
        {
            var recipe = await _context.Recipies
                .Include(r => r.Comments)
                .Include(r => r.Likes)
                .Include(r => r.SavedByUsers)
                .Include(r => r.RecipeIngredients)
                .FirstOrDefaultAsync(r => r.Id == recipeId);

            if (recipe == null) return false;

            //only the admin and the creator of the recipe have rights for this operation
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

        /// <summary>
        /// Loads list of similar recipes
        /// </summary>
        /// <param name="referenceRecipe"></param>
        /// <param name="currentUserId"></param>
        /// <param name="allowEdit"></param>
        /// <returns></returns>
        public async Task<List<RecipeCardViewModel>> GetSimilarRecipesAsync(Recipe referenceRecipe, string currentUserId, bool allowEdit = false)
        {
            var candidates = await _context.Recipies
                .Include(r => r.Likes)
                .Where(r => r.Id != referenceRecipe.Id)
                .ToListAsync();

            var referenceTitle = referenceRecipe.Title?.ToLower() ?? "";

            const int titlePoints = 2;
            const int categoryPoints = 1;
            const int preferencePoints = 1;

            // Sort the recipes by most similar properties
            var ranked = candidates
                .Select(r => new
                {
                    Recipe = r,
                    Score =
                        (referenceRecipe.IsVegetarian && r.IsVegetarian ? preferencePoints : 0) +
                        (referenceRecipe.IsVegan && r.IsVegan ? preferencePoints : 0) +
                        (referenceRecipe.IsGlutenFree && r.IsGlutenFree ? preferencePoints : 0) +
                        (referenceRecipe.IsLactoseFree && r.IsLactoseFree ? preferencePoints : 0) +
                        (r.RecipeCategoryId == referenceRecipe.RecipeCategoryId ? categoryPoints : 0) + 
                        (!string.IsNullOrEmpty(referenceTitle) && r.Title.ToLower().Contains(referenceTitle) ? titlePoints : 0)
                })
                .OrderByDescending(r => r.Score)
                .ThenByDescending(r => r.Recipe.Likes.Count)
                .Take(6)
                .ToList();

            return ranked.Select(r => new RecipeCardViewModel
            {
                Id = r.Recipe.Id,
                Title = r.Recipe.Title,
                DescriptionPreview = Regex.Replace(r.Recipe.Description ?? "", "<.*?>", "").Truncate(100),
                ImageUrl = $"/Recipes/GetImage/{r.Recipe.Id}",
                LikesCount = r.Recipe.Likes.Count,
                ViewCount = r.Recipe.ViewCount,
                IsOwner = allowEdit && currentUserId == r.Recipe.UserId
            }).ToList();
        }

        /// <summary>
        /// Loads list of suggested recipes
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="allowEdit"></param>
        /// <returns></returns>
        public async Task<List<RecipeCardViewModel>> GetSuggestedRecipesForUserAsync(string userId, bool allowEdit = false)
        {
            var recentLikedOrSavedIds = await _context.SavedRecipes
                .Where(s => s.UserId == userId)
                .OrderByDescending(s => s.Id)
                .Select(s => s.RecipeId)
                .Union(_context.RecipeLikes
                    .Where(l => l.UserId == userId)
                    .OrderByDescending(l => l.Id)
                    .Select(l => l.RecipeId))
                .Distinct()
                .Take(3)
                .ToListAsync();

            var referenceRecipes = await _context.Recipies
                .Where(r => recentLikedOrSavedIds.Contains(r.Id))
                .ToListAsync();

            if (!referenceRecipes.Any())
                return new List<RecipeCardViewModel>();

            var allCandidates = await _context.Recipies
                .Include(r => r.User)
                .Include(r => r.Likes)
                .Where(r => !referenceRecipes.Select(rr => rr.Id).Contains(r.Id))
                .ToListAsync();

            const int titlePoints = 2;
            const int categoryPoints = 1;
            const int preferencePoints = 1;

            var rankedCandidates = allCandidates
                .Select(r => new
                {
                    Recipe = r,
                    Score = referenceRecipes.Sum(reference =>
                        (reference.IsVegetarian && r.IsVegetarian ? preferencePoints : 0) +
                        (reference.IsVegan && r.IsVegan ? preferencePoints : 0) +
                        (reference.IsGlutenFree && r.IsGlutenFree ? preferencePoints : 0) +
                        (reference.IsLactoseFree && r.IsLactoseFree ? preferencePoints : 0) +
                        (reference.RecipeCategoryId == r.RecipeCategoryId ? categoryPoints : 0) +
                        (!string.IsNullOrEmpty(reference.Title) && r.Title.ToLower().Contains(reference.Title.ToLower()) ? titlePoints : 0)
                    )
                })
                .OrderByDescending(r => r.Score)
                .ThenByDescending(r => r.Recipe.Likes.Count)
                .Take(6)
                .ToList();

            return rankedCandidates.Select(r => new RecipeCardViewModel
            {
                Id = r.Recipe.Id,
                Title = r.Recipe.Title,
                DescriptionPreview = Regex.Replace(r.Recipe.Description ?? "", "<.*?>", "").Truncate(100),
                ImageUrl = $"/Recipes/GetImage/{r.Recipe.Id}",
                UserName = r.Recipe.User?.UserName ?? "l",
                UserId = r.Recipe.UserId,
                LikesCount = r.Recipe.Likes.Count,
                ViewCount = r.Recipe.ViewCount,
                IsOwner = allowEdit && userId == r.Recipe.UserId
            }).ToList();
        }

    }

}
