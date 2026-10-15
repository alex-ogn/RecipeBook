using System.Text.RegularExpressions;
using RecipeBook.Constants;
using RecipeBook.Models;
using RecipeBook.Resources;

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

        public static List<RecipeCardViewModel> ToCards(
            IEnumerable<RecipeCardSource> sources,
            string? currentUserId,
            bool isAdmin = false)
        {
            return sources.Select(source => ToCard(source, currentUserId, isAdmin)).ToList();
        }

        public static RecipeCardViewModel ToCard(
            Recipe recipe,
            string? currentUserId,
            bool isAdmin = false)
        {
            return ToCard(RecipeCardSource.FromRecipe(recipe), currentUserId, isAdmin);
        }

        public static RecipeCardViewModel ToCard(
            RecipeCardSource source,
            string? currentUserId,
            bool isAdmin = false)
        {
            return new RecipeCardViewModel
            {
                Id = source.Id,
                Title = source.Title,
                DescriptionPreview = BuildDescriptionPreview(source.Description),
                ImageUrl = string.IsNullOrWhiteSpace(source.ImageUrl)
                    ? ImageConstants.DefaultRecipeImageUrl
                    : source.ImageUrl,
                UserName = source.UserName ?? DisplayTexts.DefaultUserName,
                UserId = source.UserId,
                LikesCount = source.LikesCount,
                ViewCount = source.ViewCount,
                IsOwner = isAdmin || (!string.IsNullOrEmpty(currentUserId) && source.UserId == currentUserId)
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
