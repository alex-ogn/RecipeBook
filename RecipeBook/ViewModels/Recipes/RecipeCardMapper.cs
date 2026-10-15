using System.Text.RegularExpressions;
using RecipeBook.Constants;
using RecipeBook.Models;

namespace RecipeBook.ViewModels.Recipes
{
    public static class RecipeCardMapper
    {
        private const int PreviewLength = 100;

        private static readonly Regex HtmlTagRegex = new("<.*?>", RegexOptions.Compiled | RegexOptions.Singleline);

        public static List<RecipeCardViewModel> ToCards(
            IEnumerable<Recipe> recipes,
            string? currentUserId,
            bool isAdmin = false)
        {
            return recipes.Select(recipe => ToCard(recipe, currentUserId, isAdmin)).ToList();
        }

        public static RecipeCardViewModel ToCard(
            Recipe recipe,
            string? currentUserId,
            bool isAdmin = false)
        {
            return new RecipeCardViewModel
            {
                Id = recipe.Id,
                Title = recipe.Title,
                DescriptionPreview = BuildDescriptionPreview(recipe.Description),
                ImageUrl = string.IsNullOrWhiteSpace(recipe.ImageUrl)
                    ? ImageConstants.DefaultRecipeImageUrl
                    : recipe.ImageUrl,
                UserName = recipe.User?.UserName ?? UserProfileConstants.DefaultUserName,
                UserId = recipe.UserId,
                LikesCount = recipe.Likes?.Count ?? 0,
                ViewCount = recipe.ViewCount,
                IsOwner = isAdmin || (!string.IsNullOrEmpty(currentUserId) && recipe.UserId == currentUserId)
            };
        }

        private static string BuildDescriptionPreview(string? description)
        {
            var plainText = HtmlTagRegex.Replace(description ?? string.Empty, string.Empty);
            if (plainText.Length <= PreviewLength)
            {
                return plainText;
            }

            return plainText.Substring(0, PreviewLength) + "...";
        }
    }
}
