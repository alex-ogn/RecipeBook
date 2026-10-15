using System.ComponentModel.DataAnnotations;
using Ganss.Xss;
using Microsoft.EntityFrameworkCore;
using RecipeBook.Constants;
using RecipeBook.Data;
using RecipeBook.Models;
using RecipeBook.Resources;
using RecipeBook.ViewModels.Enums;
using RecipeBook.ViewModels.Recipes;

namespace RecipeBook.Services
{
    /// <summary>
    /// Class for managing recipes
    /// </summary>
    public class RecipeService : IRecipeService
    {
        private const int IngredientNameMaxLength = 100;
        private const int IngredientNameMinLength = 1;

        private readonly ApplicationDbContext _context;
        private readonly IImageStorageService _imageStorage;
        private readonly IHtmlSanitizer _sanitizer;

        public RecipeService(ApplicationDbContext context, IImageStorageService imageStorage, IHtmlSanitizer sanitizer)
        {
            _context = context;
            _imageStorage = imageStorage;
            _sanitizer = sanitizer;
        }

        public Task<List<Recipe>> GetRecipesAsync(RecipeListFilter filter)
        {
            var query = _context.Recipies
                .AsNoTracking()
                .Include(r => r.User)
                .Include(r => r.Likes)
                .AsQueryable();

            if (filter.CategoryId.HasValue)
            {
                query = query.Where(r => r.RecipeCategoryId == filter.CategoryId);
            }

            if (filter.AuthorId != null)
            {
                query = query.Where(r => r.UserId == filter.AuthorId);
            }

            if (filter.LikedByUserId != null)
            {
                query = query.Where(r => r.Likes.Any(l => l.UserId == filter.LikedByUserId));
            }

            if (filter.SavedByUserId != null)
            {
                query = query.Where(r => r.SavedByUsers.Any(s => s.UserId == filter.SavedByUserId));
            }

            if (filter.FollowedByUserId != null)
            {
                query = query.Where(r => _context.UserFollowers
                    .Any(f => f.FollowerId == filter.FollowedByUserId && f.FollowedId == r.UserId));
            }

            if (!string.IsNullOrWhiteSpace(filter.TitleContains))
            {
                var title = filter.TitleContains.Trim().ToLower();
                query = query.Where(r => r.Title.ToLower().Contains(title));
            }

            query = filter.Sort switch
            {
                RecipeSortOption.MostLiked => query.OrderByDescending(r => r.Likes.Count),
                RecipeSortOption.MostViewed => query.OrderByDescending(r => r.ViewCount),
                _ => query.OrderByDescending(r => r.Id)
            };

            return query.ToListAsync();
        }

        public Task<List<Recipe>> GetMostPopularAsync(int count)
            => _context.Recipies
                .AsNoTracking()
                .Include(r => r.User)
                .Include(r => r.Likes)
                .OrderByDescending(r => r.Likes.Count)
                .ThenByDescending(r => r.ViewCount)
                .Take(count)
                .ToListAsync();

        public Task<int> CountAsync() => _context.Recipies.CountAsync();

        public async Task<RecipeDetailsResult?> GetDetailsAsync(int id, string? currentUserId)
        {
            var recipe = await _context.Recipies
                .Include(r => r.RecipeIngredients)
                    .ThenInclude(ri => ri.Ingredient)
                        .ThenInclude(i => i.IngredientCategory)
                .Include(r => r.RecipeCategory)
                .Include(r => r.User)
                .Include(r => r.Comments)
                    .ThenInclude(c => c.User)
                .AsSplitQuery()
                .FirstOrDefaultAsync(r => r.Id == id);

            if (recipe == null)
            {
                return null;
            }

            return new RecipeDetailsResult
            {
                Recipe = recipe,
                SimilarRecipes = await GetSimilarRecipesAsync(recipe, currentUserId),
                IsSaved = currentUserId != null
                    && await _context.SavedRecipes.AnyAsync(s => s.RecipeId == id && s.UserId == currentUserId),
                IsLiked = currentUserId != null
                    && await _context.RecipeLikes.AnyAsync(l => l.RecipeId == id && l.UserId == currentUserId),
                LikeCount = await _context.RecipeLikes.CountAsync(l => l.RecipeId == id)
            };
        }

        public Task<Recipe?> GetForEditAsync(int id)
            => _context.Recipies
                .AsNoTracking()
                .Include(r => r.RecipeCategory)
                .Include(r => r.RecipeIngredients)
                    .ThenInclude(ri => ri.Ingredient)
                .FirstOrDefaultAsync(r => r.Id == id);

        public async Task<Recipe?> GetByIdAsync(int id) => await _context.Recipies.FindAsync(id);

        public Task<Recipe?> GetWithAuthorAsync(int id)
            => _context.Recipies
                .AsNoTracking()
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.Id == id);

        public Task<Recipe?> GetForPdfAsync(int id)
            => _context.Recipies
                .AsNoTracking()
                .Include(r => r.RecipeIngredients)
                    .ThenInclude(ri => ri.Ingredient)
                .Include(r => r.RecipeCategory)
                .FirstOrDefaultAsync(r => r.Id == id);

        public async Task CreateAsync(Recipe recipe, IReadOnlyCollection<RecipeIngredientViewModel> ingredients)
        {
            SanitizeContent(recipe);
            recipe.RecipeIngredients = await BuildRecipeIngredientsAsync(ingredients);

            _context.Recipies.Add(recipe);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Recipe recipe, IReadOnlyCollection<RecipeIngredientViewModel> ingredients)
        {
            SanitizeContent(recipe);
            var recipeIngredients = await BuildRecipeIngredientsAsync(ingredients);

            var existing = await _context.RecipeIngredients
                .Where(ri => ri.RecipeId == recipe.Id)
                .ToListAsync();
            _context.RecipeIngredients.RemoveRange(existing);

            foreach (var recipeIngredient in recipeIngredients)
            {
                recipeIngredient.RecipeId = recipe.Id;
            }

            _context.RecipeIngredients.AddRange(recipeIngredients);

            // One save for the recipe, its ingredients and any new user-added ingredients.
            await _context.SaveChangesAsync();
        }

        private void SanitizeContent(Recipe recipe)
        {
            recipe.Description = _sanitizer.Sanitize(recipe.Description ?? string.Empty);
            recipe.Instructions = _sanitizer.Sanitize(recipe.Instructions ?? string.Empty);
        }

        /// <summary>
        /// Turns the ingredients posted by the recipe form into RecipeIngredient rows. New ingredients typed by the
        /// user are added to the "added by user" category (reusing an existing one with the same name); nothing is
        /// saved here, so the caller's single SaveChanges stores everything together.
        /// </summary>
        private async Task<List<RecipeIngredient>> BuildRecipeIngredientsAsync(IReadOnlyCollection<RecipeIngredientViewModel> items)
        {
            foreach (var item in items)
            {
                Validator.ValidateObject(item, new ValidationContext(item), validateAllProperties: true);

                if (item.IsNew && string.IsNullOrWhiteSpace(item.Name))
                {
                    throw new ValidationException(string.Format(
                        ValidationTexts.IngredientNameLength, string.Empty, IngredientNameMaxLength, IngredientNameMinLength));
                }
            }

            var newIngredientsByName = await LoadAddedByUserIngredientsAsync(items);
            var result = new List<RecipeIngredient>();

            foreach (var item in items)
            {
                if (!item.IsNew)
                {
                    result.Add(new RecipeIngredient { IngredientId = item.IngredientId, QuantityNeeded = item.QuantityNeeded });
                    continue;
                }

                var name = item.Name.Trim();
                if (!newIngredientsByName.Ingredients.TryGetValue(name, out var ingredient))
                {
                    ingredient = new Ingredient { Name = name, IngredientCategoryId = newIngredientsByName.CategoryId };
                    _context.Ingredients.Add(ingredient);
                    newIngredientsByName.Ingredients[name] = ingredient;
                }

                result.Add(new RecipeIngredient { Ingredient = ingredient, QuantityNeeded = item.QuantityNeeded });
            }

            return result;
        }

        /// <summary>
        /// Loads the "added by user" category and its ingredients whose names match the new ingredients.
        /// </summary>
        private async Task<(int CategoryId, Dictionary<string, Ingredient> Ingredients)> LoadAddedByUserIngredientsAsync(
            IReadOnlyCollection<RecipeIngredientViewModel> items)
        {
            var byName = new Dictionary<string, Ingredient>(StringComparer.OrdinalIgnoreCase);
            var newNames = items
                .Where(i => i.IsNew)
                .Select(i => i.Name.Trim().ToLower())
                .Distinct()
                .ToList();

            if (newNames.Count == 0)
            {
                return (0, byName);
            }

            var categoryId = await _context.IngredientCategories
                .Where(c => c.Name == IngredientCategoryConstants.AddedByUserCategoryName)
                .Select(c => (int?)c.Id)
                .FirstOrDefaultAsync()
                ?? throw new InvalidOperationException("Default ingredient category not found.");

            var existing = await _context.Ingredients
                .Where(i => i.IngredientCategoryId == categoryId && newNames.Contains(i.Name.ToLower()))
                .ToListAsync();

            foreach (var ingredient in existing)
            {
                byName.TryAdd(ingredient.Name, ingredient);
            }

            return (categoryId, byName);
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

            // Remove the file only after the recipe is gone, so a failed save never leaves a recipe without its image.
            _imageStorage.DeleteRecipeImage(recipe.ImageUrl);

            return true;
        }

        public async Task<bool> RegisterViewAsync(Recipe recipe, string? viewerId)
        {
            if (recipe.UserId == viewerId)
            {
                return false;
            }

            // The recipe is tracked, so only the ViewCount column is updated.
            recipe.ViewCount++;
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
        public async Task<List<RecipeCardViewModel>> GetSimilarRecipesAsync(Recipe referenceRecipe, string? currentUserId, bool allowEdit = false)
        {
            var candidates = await _context.Recipies
                .AsNoTracking()
                .Include(r => r.User)
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

            return RecipeCardMapper.ToCards(ranked.Select(r => r.Recipe), allowEdit ? currentUserId : null);
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

            return RecipeCardMapper.ToCards(rankedCandidates.Select(r => r.Recipe), allowEdit ? userId : null);
        }

    }

}
