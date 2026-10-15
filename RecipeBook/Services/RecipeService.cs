using System.ComponentModel.DataAnnotations;
using Ganss.Xss;
using Microsoft.EntityFrameworkCore;
using RecipeBook.Constants;
using RecipeBook.Data;
using RecipeBook.Extensions;
using RecipeBook.Models;
using RecipeBook.Resources;
using RecipeBook.ViewModels.Enums;
using RecipeBook.ViewModels.Recipes;
using RecipeBook.ViewModels.Shared;

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

        public Task<PagedResult<RecipeCardSource>> GetRecipeCardsAsync(RecipeListFilter filter, int page, int pageSize)
            => ApplyFilter(filter)
                .Select(RecipeCardSource.Projection)
                .ToPagedResultAsync(page, pageSize);

        private IQueryable<Recipe> ApplyFilter(RecipeListFilter filter)
        {
            var query = _context.Recipes.AsNoTracking();

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

            // The Id tie-breaker keeps pages stable when many recipes share a like or view count.
            return filter.Sort switch
            {
                RecipeSortOption.MostLiked => query.OrderByDescending(r => r.Likes.Count).ThenByDescending(r => r.Id),
                RecipeSortOption.MostViewed => query.OrderByDescending(r => r.ViewCount).ThenByDescending(r => r.Id),
                _ => query.OrderByDescending(r => r.Id)
            };
        }

        public Task<List<RecipeCardSource>> GetMostPopularAsync(int count)
            => _context.Recipes
                .AsNoTracking()
                .OrderByDescending(r => r.Likes.Count)
                .ThenByDescending(r => r.ViewCount)
                .Take(count)
                .Select(RecipeCardSource.Projection)
                .ToListAsync();

        public Task<int> CountAsync() => _context.Recipes.CountAsync();

        public async Task<RecipeDetailsResult?> GetDetailsAsync(int id, string? currentUserId)
        {
            var recipe = await _context.Recipes
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
            => _context.Recipes
                .AsNoTracking()
                .Include(r => r.RecipeCategory)
                .Include(r => r.RecipeIngredients)
                    .ThenInclude(ri => ri.Ingredient)
                .FirstOrDefaultAsync(r => r.Id == id);

        public async Task<Recipe?> GetByIdAsync(int id) => await _context.Recipes.FindAsync(id);

        public Task<Recipe?> GetWithAuthorAsync(int id)
            => _context.Recipes
                .AsNoTracking()
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.Id == id);

        public Task<Recipe?> GetForPdfAsync(int id)
            => _context.Recipes
                .AsNoTracking()
                .Include(r => r.RecipeIngredients)
                    .ThenInclude(ri => ri.Ingredient)
                .Include(r => r.RecipeCategory)
                .FirstOrDefaultAsync(r => r.Id == id);

        public async Task CreateAsync(Recipe recipe, IReadOnlyCollection<RecipeIngredientViewModel> ingredients)
        {
            SanitizeContent(recipe);
            recipe.RecipeIngredients = await BuildRecipeIngredientsAsync(ingredients);

            _context.Recipes.Add(recipe);
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
            var recipe = await _context.Recipes
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

            _context.Recipes.Remove(recipe);
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
            var candidates = _context.Recipes.Where(r => r.Id != referenceRecipe.Id);
            var ranked = await RankBySimilarityAsync(candidates, new[] { referenceRecipe });

            return RecipeCardMapper.ToCards(ranked, allowEdit ? currentUserId : null);
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

            var referenceRecipes = await _context.Recipes
                .AsNoTracking()
                .Where(r => recentLikedOrSavedIds.Contains(r.Id))
                .ToListAsync();

            if (!referenceRecipes.Any())
                return new List<RecipeCardViewModel>();

            var referenceIds = referenceRecipes.Select(r => r.Id).ToList();
            var candidates = _context.Recipes.Where(r => !referenceIds.Contains(r.Id));
            var ranked = await RankBySimilarityAsync(candidates, referenceRecipes);

            return RecipeCardMapper.ToCards(ranked, allowEdit ? userId : null);
        }

        private const int SimilarRecipesCount = 6;
        private const int MaxReferenceRecipes = 3;
        private const int TitlePoints = 2;
        private const int CategoryPoints = 1;
        private const int PreferencePoints = 1;

        /// <summary>
        /// Scores candidates against up to <see cref="MaxReferenceRecipes"/> reference recipes and returns the best
        /// matches (ties broken by likes). The score is summed over the references: <see cref="PreferencePoints"/>
        /// per shared diet flag, <see cref="CategoryPoints"/> for the same category and <see cref="TitlePoints"/>
        /// when the reference title appears in the candidate's title. Scoring and sorting run in the database.
        /// </summary>
        private async Task<List<RecipeCardSource>> RankBySimilarityAsync(IQueryable<Recipe> candidates, IReadOnlyList<Recipe> references)
        {
            references = references.Take(MaxReferenceRecipes).ToList();

            // How many references share each diet flag: a candidate with the flag gets that many points.
            var vegetarian = references.Count(r => r.IsVegetarian) * PreferencePoints;
            var vegan = references.Count(r => r.IsVegan) * PreferencePoints;
            var glutenFree = references.Count(r => r.IsGlutenFree) * PreferencePoints;
            var lactoseFree = references.Count(r => r.IsLactoseFree) * PreferencePoints;

            // Fixed number of slots so the query shape is the same for 1, 2 or 3 references.
            // An empty slot uses an impossible category id and a null title, which never match.
            int CategoryAt(int i) => i < references.Count ? references[i].RecipeCategoryId : -1;
            string? TitleAt(int i) => i < references.Count && !string.IsNullOrEmpty(references[i].Title)
                ? references[i].Title.ToLower()
                : null;

            var category0 = CategoryAt(0);
            var category1 = CategoryAt(1);
            var category2 = CategoryAt(2);
            var title0 = TitleAt(0);
            var title1 = TitleAt(1);
            var title2 = TitleAt(2);

            var ranked = await candidates
                .AsNoTracking()
                .Select(r => new
                {
                    r.Id,
                    r.Title,
                    r.Description,
                    r.ImageUrl,
                    r.UserId,
                    UserName = r.User != null ? r.User.UserName : null,
                    LikesCount = r.Likes.Count,
                    r.ViewCount,
                    Score =
                        (r.IsVegetarian ? vegetarian : 0) +
                        (r.IsVegan ? vegan : 0) +
                        (r.IsGlutenFree ? glutenFree : 0) +
                        (r.IsLactoseFree ? lactoseFree : 0) +
                        (r.RecipeCategoryId == category0 ? CategoryPoints : 0) +
                        (r.RecipeCategoryId == category1 ? CategoryPoints : 0) +
                        (r.RecipeCategoryId == category2 ? CategoryPoints : 0) +
                        (title0 != null && r.Title.ToLower().Contains(title0) ? TitlePoints : 0) +
                        (title1 != null && r.Title.ToLower().Contains(title1) ? TitlePoints : 0) +
                        (title2 != null && r.Title.ToLower().Contains(title2) ? TitlePoints : 0)
                })
                .OrderByDescending(r => r.Score)
                .ThenByDescending(r => r.LikesCount)
                .ThenByDescending(r => r.Id)
                .Take(SimilarRecipesCount)
                .ToListAsync();

            return ranked
                .Select(r => new RecipeCardSource(r.Id, r.Title, r.Description, r.ImageUrl, r.UserId, r.UserName, r.LikesCount, r.ViewCount))
                .ToList();
        }

    }

}
