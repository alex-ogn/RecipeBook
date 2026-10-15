using System.ComponentModel.DataAnnotations;
using Moq;
using RecipeBook.Constants;
using RecipeBook.Models;
using RecipeBook.Services;
using RecipeBook.ViewModels.Enums;
using RecipeBook.ViewModels.Recipes;

namespace Tests.Services
{
    public class RecipeServiceListAndSaveTests
    {
        private const int AddedByUserCategoryId = 7;

        private static RecipeService CreateService(RecipeBook.Data.ApplicationDbContext context)
            => new(context, new Mock<IImageStorageService>().Object, RecipeHtmlSanitizer.Create());

        private static async Task<RecipeBook.Data.ApplicationDbContext> SeedListAsync()
        {
            var context = TestDb.Create();
            context.Users.AddRange(TestDb.User("me"), TestDb.User("chef"), TestDb.User("other"));
            context.RecipeCategories.AddRange(
                new RecipeCategory { Id = 1, Name = "Soups" },
                new RecipeCategory { Id = 2, Name = "Desserts" });
            context.Recipes.AddRange(
                TestDb.Recipe(1, "chef", title: "Tomato Soup", categoryId: 1),
                TestDb.Recipe(2, "other", title: "Apple Pie", categoryId: 2),
                TestDb.Recipe(3, "chef", title: "Bean Soup", categoryId: 1));
            context.RecipeLikes.Add(new RecipeLike { RecipeId = 2, UserId = "me" });
            context.SavedRecipes.Add(new SavedRecipe { RecipeId = 3, UserId = "me" });
            context.UserFollowers.Add(new UserFollower { FollowerId = "me", FollowedId = "chef" });
            await context.SaveChangesAsync();
            return context;
        }

        [Fact]
        public async Task GetRecipes_FiltersByEachCriterion()
        {
            using var context = await SeedListAsync();
            var service = CreateService(context);

            async Task<int[]> Ids(RecipeListFilter filter) => (await service.GetRecipeCardsAsync(filter, 1, 100)).Select(r => r.Id).OrderBy(id => id).ToArray();

            Assert.Equal(new[] { 1, 3 }, await Ids(new RecipeListFilter { CategoryId = 1 }));
            Assert.Equal(new[] { 1, 3 }, await Ids(new RecipeListFilter { AuthorId = "chef" }));
            Assert.Equal(new[] { 2 }, await Ids(new RecipeListFilter { LikedByUserId = "me" }));
            Assert.Equal(new[] { 3 }, await Ids(new RecipeListFilter { SavedByUserId = "me" }));
            Assert.Equal(new[] { 1, 3 }, await Ids(new RecipeListFilter { FollowedByUserId = "me" }));
            Assert.Equal(new[] { 1, 3 }, await Ids(new RecipeListFilter { TitleContains = "SOUP" }));
        }

        [Fact]
        public async Task GetRecipes_IncludesAuthorAndSorts()
        {
            using var context = await SeedListAsync();
            var service = CreateService(context);

            var newest = await service.GetRecipeCardsAsync(new RecipeListFilter(), 1, 100);
            var mostLiked = await service.GetRecipeCardsAsync(new RecipeListFilter { Sort = RecipeSortOption.MostLiked }, 1, 100);

            Assert.Equal(new[] { 3, 2, 1 }, newest.Select(r => r.Id));
            Assert.Equal(2, mostLiked.First().Id);
            Assert.Equal(1, mostLiked.First().LikesCount);
            Assert.All(newest, r => Assert.NotNull(r.UserName));
        }

        [Fact]
        public async Task GetRecipeCards_ReturnsRequestedPage_AndClampsOutOfRangePages()
        {
            using var context = await SeedListAsync();
            var service = CreateService(context);

            var second = await service.GetRecipeCardsAsync(new RecipeListFilter(), page: 2, pageSize: 2);
            var tooFar = await service.GetRecipeCardsAsync(new RecipeListFilter(), page: 50, pageSize: 2);
            var tooLow = await service.GetRecipeCardsAsync(new RecipeListFilter(), page: 0, pageSize: 2);

            Assert.Equal(new[] { 1 }, second.Select(r => r.Id));
            Assert.Equal(3, second.TotalCount);
            Assert.Equal(2, second.TotalPages);
            Assert.Equal(2, tooFar.Page);
            Assert.Equal(1, tooLow.Page);
            Assert.Equal(new[] { 3, 2 }, tooLow.Select(r => r.Id));
        }

        [Fact]
        public async Task GetSimilarRecipes_RanksByCategoryTitleAndDietFlags()
        {
            using var context = await SeedListAsync();
            var reference = TestDb.Recipe(4, "other", title: "Soup", categoryId: 1);
            reference.IsVegan = true;
            context.Recipes.Add(reference);
            var veganPie = await context.Recipes.FindAsync(2);
            veganPie!.IsVegan = true;
            await context.SaveChangesAsync();
            var service = CreateService(context);

            var similar = await service.GetSimilarRecipesAsync(reference, currentUserId: null);

            // Soups share the category (+1) and title (+2); the vegan pie shares only the diet flag (+1).
            Assert.Equal(new[] { 3, 1, 2 }, similar.Select(c => c.Id));
            Assert.DoesNotContain(similar, c => c.Id == 4);
        }

        [Fact]
        public async Task GetSuggestedRecipes_UsesLikedAndSavedRecipesAsReferences()
        {
            using var context = await SeedListAsync();
            context.Recipes.Add(TestDb.Recipe(5, "other", title: "Apple Pie Deluxe", categoryId: 2));
            await context.SaveChangesAsync();
            var service = CreateService(context);

            // "me" liked recipe 2 (Apple Pie) and saved recipe 3 (Bean Soup).
            var suggestions = await service.GetSuggestedRecipesForUserAsync("me");

            Assert.Equal(5, suggestions.First().Id);
            Assert.DoesNotContain(suggestions, c => c.Id == 2 || c.Id == 3);
        }

        [Fact]
        public async Task GetDetails_ReturnsLikeAndSaveStateForCurrentUser()
        {
            using var context = await SeedListAsync();
            var service = CreateService(context);

            var details = await service.GetDetailsAsync(2, "me");

            Assert.NotNull(details);
            Assert.True(details!.IsLiked);
            Assert.False(details.IsSaved);
            Assert.Equal(1, details.LikeCount);
            Assert.Null(await service.GetDetailsAsync(99, "me"));
        }

        [Fact]
        public async Task Create_SanitizesHtml_AndAddsNewIngredientsOnce()
        {
            using var context = TestDb.Create();
            context.IngredientCategories.Add(new IngredientCategory { Id = AddedByUserCategoryId, Name = IngredientCategoryConstants.AddedByUserCategoryName });
            context.Ingredients.Add(new Ingredient { Id = 50, Name = "Salt", IngredientCategoryId = 1 });
            await context.SaveChangesAsync();
            var service = CreateService(context);

            var recipe = TestDb.Recipe(0, "chef");
            recipe.Description = "<p>Nice</p><script>alert(1)</script>";

            await service.CreateAsync(recipe, new List<RecipeIngredientViewModel>
            {
                new() { IngredientId = 50, Name = "Salt", QuantityNeeded = "1 tsp" },
                new() { IsNew = true, Name = "Saffron", QuantityNeeded = "a pinch" },
                new() { IsNew = true, Name = " saffron ", QuantityNeeded = "more" }
            });

            var saved = context.Recipes.Single();
            Assert.DoesNotContain("<script>", saved.Description);
            Assert.Contains("<p>Nice</p>", saved.Description);

            var saffron = Assert.Single(context.Ingredients, i => i.Name == "Saffron");
            Assert.Equal(AddedByUserCategoryId, saffron.IngredientCategoryId);
            Assert.Equal(3, context.RecipeIngredients.Count(ri => ri.RecipeId == saved.Id));
        }

        [Fact]
        public async Task Create_ReusesExistingUserAddedIngredient()
        {
            using var context = TestDb.Create();
            context.IngredientCategories.Add(new IngredientCategory { Id = AddedByUserCategoryId, Name = IngredientCategoryConstants.AddedByUserCategoryName });
            context.Ingredients.Add(new Ingredient { Id = 60, Name = "Saffron", IngredientCategoryId = AddedByUserCategoryId });
            await context.SaveChangesAsync();
            var service = CreateService(context);

            await service.CreateAsync(TestDb.Recipe(0, "chef"), new List<RecipeIngredientViewModel>
            {
                new() { IsNew = true, Name = "saffron", QuantityNeeded = "a pinch" }
            });

            Assert.Single(context.Ingredients);
            Assert.Equal(60, context.RecipeIngredients.Single().IngredientId);
        }

        [Fact]
        public async Task Create_RejectsNewIngredientWithoutName()
        {
            using var context = TestDb.Create();
            var service = CreateService(context);

            await Assert.ThrowsAsync<ValidationException>(() => service.CreateAsync(TestDb.Recipe(0, "chef"),
                new List<RecipeIngredientViewModel> { new() { IsNew = true, Name = " ", QuantityNeeded = "1" } }));
            Assert.Empty(context.Recipes);
        }

        [Fact]
        public async Task Update_ReplacesIngredients()
        {
            using var context = TestDb.Create();
            context.Recipes.Add(TestDb.Recipe(1, "chef"));
            context.Ingredients.AddRange(
                new Ingredient { Id = 1, Name = "Salt", IngredientCategoryId = 1 },
                new Ingredient { Id = 2, Name = "Pepper", IngredientCategoryId = 1 });
            context.RecipeIngredients.Add(new RecipeIngredient { RecipeId = 1, IngredientId = 1, QuantityNeeded = "1" });
            await context.SaveChangesAsync();
            var service = CreateService(context);

            var recipe = (await service.GetByIdAsync(1))!;
            recipe.Title = "Updated";
            await service.UpdateAsync(recipe, new List<RecipeIngredientViewModel>
            {
                new() { IngredientId = 2, Name = "Pepper", QuantityNeeded = "2" }
            });

            Assert.Equal("Updated", context.Recipes.Single().Title);
            var ingredient = Assert.Single(context.RecipeIngredients);
            Assert.Equal(2, ingredient.IngredientId);
        }
    }
}
