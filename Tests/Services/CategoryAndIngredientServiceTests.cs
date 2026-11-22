using RecipeBook.Constants;
using RecipeBook.Models;
using RecipeBook.Services;

namespace Tests.Services
{
    public class CategoryAndIngredientServiceTests
    {
        [Fact]
        public async Task RecipeCategory_NameExists_IgnoresTheCategoryBeingEdited()
        {
            using var context = TestDb.Create();
            context.RecipeCategories.Add(new RecipeCategory { Id = 1, Name = "Soups" });
            await context.SaveChangesAsync();
            var service = new RecipeCategoryService(context);

            Assert.True(await service.NameExistsAsync("Soups"));
            Assert.False(await service.NameExistsAsync("Soups", excludeId: 1));
        }

        [Fact]
        public async Task RecipeCategory_WithRecipeCounts_SkipsEmptyCategories_AndPutsTheLargestFirst()
        {
            using var context = TestDb.Create();
            context.RecipeCategories.AddRange(
                new RecipeCategory { Id = 1, Name = "Soups" },
                new RecipeCategory { Id = 2, Name = "Desserts" },
                new RecipeCategory { Id = 3, Name = "Empty" },
                new RecipeCategory { Id = 4, Name = "Salads" });
            context.Recipes.AddRange(
                TestDb.Recipe(10, "u1", categoryId: 1),
                TestDb.Recipe(11, "u1", categoryId: 2),
                TestDb.Recipe(12, "u1", categoryId: 2),
                TestDb.Recipe(13, "u1", categoryId: 4));
            await context.SaveChangesAsync();
            var service = new RecipeCategoryService(context);

            var all = await service.GetWithRecipeCountsAsync(count: 10);
            var top2 = await service.GetWithRecipeCountsAsync(count: 2);

            // Same count (Soups, Salads): the lower Id comes first.
            Assert.Equal(new[] { (2, 2), (1, 1), (4, 1) }, all.Select(c => (c.Id, c.RecipeCount)));
            Assert.Equal(new[] { 2, 1 }, top2.Select(c => c.Id));
        }

        [Fact]
        public async Task RecipeCategory_WithRecipeCounts_TakesThePhotoOfTheMostLikedRecipeThatHasOne()
        {
            using var context = TestDb.Create();
            context.RecipeCategories.AddRange(
                new RecipeCategory { Id = 1, Name = "Soups" },
                new RecipeCategory { Id = 2, Name = "Desserts" });

            var likedWithoutPhoto = TestDb.Recipe(10, "u1", categoryId: 1);
            var likedWithPhoto = TestDb.Recipe(11, "u1", categoryId: 1);
            likedWithPhoto.ImageUrl = ImageConstants.RecipeImagesUrlPrefix + "liked.jpg";
            var newerWithPhoto = TestDb.Recipe(12, "u1", categoryId: 1);
            newerWithPhoto.ImageUrl = ImageConstants.RecipeImagesUrlPrefix + "newer.jpg";
            var dessertWithEmptyPhoto = TestDb.Recipe(13, "u1", categoryId: 2);
            dessertWithEmptyPhoto.ImageUrl = string.Empty;
            context.Recipes.AddRange(likedWithoutPhoto, likedWithPhoto, newerWithPhoto, dessertWithEmptyPhoto);

            // Recipe 10 is the most liked but has no photo, so the soups tile uses recipe 11 (more likes than 12).
            context.RecipeLikes.AddRange(
                new RecipeLike { RecipeId = 10, UserId = "a" },
                new RecipeLike { RecipeId = 10, UserId = "b" },
                new RecipeLike { RecipeId = 11, UserId = "a" });
            await context.SaveChangesAsync();
            var service = new RecipeCategoryService(context);

            var tiles = (await service.GetWithRecipeCountsAsync(count: 10)).ToDictionary(c => c.Id);

            Assert.Equal(likedWithPhoto.ImageUrl, tiles[1].ImageUrl);
            Assert.Null(tiles[2].ImageUrl);
        }

        [Fact]
        public async Task RecipeCategory_Delete_ReportsRecipesThatUseIt()
        {
            using var context = TestDb.Create();
            context.RecipeCategories.Add(new RecipeCategory { Id = 1, Name = "Soups" });
            context.Recipes.Add(TestDb.Recipe(10, "u1", title: "Tomato soup", categoryId: 1));
            await context.SaveChangesAsync();
            var service = new RecipeCategoryService(context);

            var result = await service.DeleteAsync(1);

            Assert.Equal(DeleteStatus.InUse, result.Status);
            Assert.Equal(new[] { "Tomato soup" }, result.UsedBy);
            Assert.NotNull(await context.RecipeCategories.FindAsync(1));
        }

        [Fact]
        public async Task RecipeCategory_Delete_RemovesUnusedCategory()
        {
            using var context = TestDb.Create();
            context.RecipeCategories.Add(new RecipeCategory { Id = 1, Name = "Soups" });
            await context.SaveChangesAsync();
            var service = new RecipeCategoryService(context);

            Assert.Equal(DeleteStatus.Deleted, (await service.DeleteAsync(1)).Status);
            Assert.Equal(DeleteStatus.NotFound, (await service.DeleteAsync(1)).Status);
        }

        [Fact]
        public async Task RecipeCategory_FindByNamePart_IsCaseInsensitive()
        {
            using var context = TestDb.Create();
            context.RecipeCategories.Add(new RecipeCategory { Id = 1, Name = "Супи" });
            await context.SaveChangesAsync();
            var service = new RecipeCategoryService(context);

            Assert.Equal(1, (await service.FindByNamePartAsync("СУП"))?.Id);
            Assert.Null(await service.FindByNamePartAsync("десерт"));
        }

        [Fact]
        public async Task IngredientCategory_Delete_ReportsIngredientsThatUseIt()
        {
            using var context = TestDb.Create();
            context.IngredientCategories.Add(new IngredientCategory { Id = 1, Name = "Vegetables" });
            context.Ingredients.Add(new Ingredient { Id = 5, Name = "Carrot", IngredientCategoryId = 1 });
            await context.SaveChangesAsync();
            var service = new IngredientCategoryService(context);

            var result = await service.DeleteAsync(1);

            Assert.Equal(DeleteStatus.InUse, result.Status);
            Assert.Equal(new[] { "Carrot" }, result.UsedBy);
        }

        [Fact]
        public async Task Ingredient_ExistsInCategory_ChecksNameAndCategory()
        {
            using var context = TestDb.Create();
            context.Ingredients.Add(new Ingredient { Id = 5, Name = "Salt", IngredientCategoryId = 1 });
            await context.SaveChangesAsync();
            var service = new IngredientService(context);

            Assert.True(await service.ExistsInCategoryAsync("Salt", 1));
            Assert.False(await service.ExistsInCategoryAsync("Salt", 2));
            Assert.False(await service.ExistsInCategoryAsync("Salt", 1, excludeId: 5));
        }

        [Fact]
        public async Task Ingredient_Delete_ReportsRecipesThatUseIt()
        {
            using var context = TestDb.Create();
            context.Recipes.Add(TestDb.Recipe(10, "u1", title: "Bread"));
            context.Ingredients.Add(new Ingredient { Id = 5, Name = "Salt", IngredientCategoryId = 1 });
            context.RecipeIngredients.Add(new RecipeIngredient { RecipeId = 10, IngredientId = 5, QuantityNeeded = "1 tsp" });
            await context.SaveChangesAsync();
            var service = new IngredientService(context);

            var result = await service.DeleteAsync(5);

            Assert.Equal(DeleteStatus.InUse, result.Status);
            Assert.Equal(new[] { "Bread" }, result.UsedBy);
        }

        [Fact]
        public async Task Ingredient_Search_FiltersByNameAndCategory()
        {
            using var context = TestDb.Create();
            context.IngredientCategories.AddRange(
                new IngredientCategory { Id = 1, Name = "Spices" },
                new IngredientCategory { Id = 2, Name = "Dairy" });
            context.Ingredients.AddRange(
                new Ingredient { Id = 1, Name = "Salt", IngredientCategoryId = 1 },
                new Ingredient { Id = 2, Name = "Sea salt", IngredientCategoryId = 2 },
                new Ingredient { Id = 3, Name = "Milk", IngredientCategoryId = 2 });
            await context.SaveChangesAsync();
            var service = new IngredientService(context);

            var result = await service.SearchAsync("alt", categoryId: 2, page: 1, pageSize: 10);

            Assert.Equal(new[] { "Sea salt" }, result.Select(i => i.Name));
            Assert.Equal(1, result.TotalCount);
        }
    }
}
