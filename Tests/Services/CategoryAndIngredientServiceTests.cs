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
        public async Task RecipeCategory_Delete_ReportsRecipesThatUseIt()
        {
            using var context = TestDb.Create();
            context.RecipeCategories.Add(new RecipeCategory { Id = 1, Name = "Soups" });
            context.Recipies.Add(TestDb.Recipe(10, "u1", title: "Tomato soup", categoryId: 1));
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
            context.Recipies.Add(TestDb.Recipe(10, "u1", title: "Bread"));
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

            var result = await service.SearchAsync("alt", categoryId: 2);

            Assert.Equal(new[] { "Sea salt" }, result.Select(i => i.Name));
        }
    }
}
