using Microsoft.EntityFrameworkCore;
using RecipeBook.Data;
using RecipeBook.Models;

namespace Tests
{
    /// <summary>
    /// Fresh in-memory database per test, plus builders for valid entities.
    /// </summary>
    internal static class TestDb
    {
        public static ApplicationDbContext Create()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            return new ApplicationDbContext(options);
        }

        public static Recipe Recipe(int id, string userId, string title = "Test Recipe", int categoryId = 1) => new()
        {
            Id = id,
            UserId = userId,
            Title = title,
            Description = "A valid test recipe",
            Instructions = "Step 1",
            Servings = 2,
            CookingTime = 30,
            RecipeCategoryId = categoryId,
            CreatedAt = DateTime.UtcNow
        };

        public static ApplicationUser User(string id) => new() { Id = id, UserName = id, Email = $"{id}@test.local" };
    }
}
