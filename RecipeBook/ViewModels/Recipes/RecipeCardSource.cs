using System.Linq.Expressions;
using RecipeBook.Models;

namespace RecipeBook.ViewModels.Recipes
{
    /// <summary>
    /// The columns a recipe card needs, selected straight from the database so that lists
    /// don't load every like and the whole user row just to show a count and a name.
    /// </summary>
    public record RecipeCardSource(
        int Id,
        string Title,
        string? Description,
        string? ImageUrl,
        string UserId,
        string? UserName,
        int LikesCount,
        int ViewCount)
    {
        /// <summary>
        /// EF Core projection: use with <c>query.Select(RecipeCardSource.Projection)</c>.
        /// </summary>
        public static readonly Expression<Func<Recipe, RecipeCardSource>> Projection = r => new RecipeCardSource(
            r.Id,
            r.Title,
            r.Description,
            r.ImageUrl,
            r.UserId,
            r.User != null ? r.User.UserName : null,
            r.Likes.Count,
            r.ViewCount);

        /// <summary>
        /// For recipes already loaded in memory (with User and Likes when available).
        /// </summary>
        public static RecipeCardSource FromRecipe(Recipe recipe) => new(
            recipe.Id,
            recipe.Title,
            recipe.Description,
            recipe.ImageUrl,
            recipe.UserId,
            recipe.User?.UserName,
            recipe.Likes?.Count ?? 0,
            recipe.ViewCount);
    }
}
