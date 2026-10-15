using RecipeBook.Constants;
using RecipeBook.Models;
using RecipeBook.ViewModels.Recipes;

namespace Tests.ViewModels
{
    public class RecipeCardMapperTests
    {
        [Fact]
        public void ToCard_UsesStoredImageUrl()
        {
            var recipe = CreateRecipe(imageUrl: "/images/recipes/abc.jpg");

            var card = RecipeCardMapper.ToCard(recipe, currentUserId: "user1");

            Assert.Equal("/images/recipes/abc.jpg", card.ImageUrl);
        }

        [Fact]
        public void ToCard_FallsBackToDefaultImage_WhenImageUrlMissing()
        {
            var recipe = CreateRecipe(imageUrl: null);

            var card = RecipeCardMapper.ToCard(recipe, currentUserId: "user1");

            Assert.Equal(ImageConstants.DefaultRecipeImageUrl, card.ImageUrl);
        }

        [Fact]
        public void ToCard_StripsHtmlOnceAndTruncatesPreview()
        {
            var recipe = CreateRecipe(
                description: "<p>" + new string('a', 80) + "</p><b>" + new string('b', 40) + "</b>");

            var card = RecipeCardMapper.ToCard(recipe, currentUserId: "user1");

            Assert.DoesNotContain("<", card.DescriptionPreview);
            Assert.DoesNotContain(">", card.DescriptionPreview);
            Assert.Equal(103, card.DescriptionPreview.Length);
            Assert.EndsWith("...", card.DescriptionPreview);
        }

        [Fact]
        public void ToCard_SetsIsOwner_ForOwnerAndAdmin()
        {
            var recipe = CreateRecipe(userId: "author");

            var ownerCard = RecipeCardMapper.ToCard(recipe, currentUserId: "author");
            var adminCard = RecipeCardMapper.ToCard(recipe, currentUserId: "other", isAdmin: true);
            var strangerCard = RecipeCardMapper.ToCard(recipe, currentUserId: "other");

            Assert.True(ownerCard.IsOwner);
            Assert.True(adminCard.IsOwner);
            Assert.False(strangerCard.IsOwner);
        }

        [Fact]
        public void ToCard_UsesUserNameWithFallback()
        {
            var named = CreateRecipe();
            named.User = new ApplicationUser { Id = "user1", UserName = "Chef" };
            var unnamed = CreateRecipe();

            Assert.Equal("Chef", RecipeCardMapper.ToCard(named, "user1").UserName);
            Assert.Equal(UserProfileConstants.DefaultUserName, RecipeCardMapper.ToCard(unnamed, "user1").UserName);
        }

        private static Recipe CreateRecipe(string userId = "user1", string? imageUrl = "/images/recipes/abc.jpg", string? description = "Plain text")
        {
            return new Recipe
            {
                Id = 1,
                Title = "Soup",
                Description = description ?? "Plain text",
                Instructions = "Cook",
                Servings = 1,
                CookingTime = 10,
                UserId = userId,
                RecipeCategoryId = 1,
                ImageUrl = imageUrl,
                ViewCount = 3,
                Likes = new List<RecipeLike> { new RecipeLike() }
            };
        }
    }
}
