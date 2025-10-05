using Microsoft.EntityFrameworkCore;
using RecipeBook.Data;
using RecipeBook.Models;
using RecipeBook.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tests.Services
{
    public class StatisticsServiceTests
    {
        private readonly StatisticsService _service;
        private readonly ApplicationDbContext _context;

        public StatisticsServiceTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: System.Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);
            _service = new StatisticsService(_context);
        }

        [Fact]
        public async Task GetUserStatisticsAsync_ShouldReturnAllAndInactiveUsers()
        {
            var user1 = new ApplicationUser { Id = "u1", UserName = "activeUser" };
            var user2 = new ApplicationUser { Id = "u2", UserName = "inactiveUser" };

            var recipe = new Recipe
            {
                Id = 1,
                UserId = "u1",
                Title = "Test Recipe",
                Description = "Desc",
                Instructions = "Cook",
                Servings = 2,
                CookingTime = 30,
                RecipeCategoryId = 1
            };

            _context.Users.AddRange(user1, user2);
            _context.Recipies.Add(recipe);
            _context.RecipeLikes.Add(new RecipeLike { RecipeId = 1, UserId = "u1" });
            _context.SavedRecipes.Add(new SavedRecipe { RecipeId = 1, UserId = "u1" });

            await _context.SaveChangesAsync();

            var result = await _service.GetUserStatisticsAsync();

            Assert.Equal(2, result.AllUsers.Count);
            Assert.Single(result.InactiveUsers);
            Assert.Equal("inactiveUser", result.InactiveUsers.First().UserName);
        }

        [Fact]
        public async Task GetRecipeStatsAsync_ShouldReturnCorrectDistributions()
        {
            var category = new RecipeCategory { Id = 1, Name = "Category A" };
            var recipe = new Recipe
            {
                Id = 1,
                UserId = "u1",
                Title = "Recipe 1",
                Description = "Desc",
                Instructions = "Instructions",
                Servings = 2,
                CookingTime = 30,
                RecipeCategoryId = 1,
                CreatedAt = System.DateTime.Today.AddHours(10),
                Comments = new List<RecipeComment>
            {
                new RecipeComment { Content = "Nice", UserId = "u1" }
            },
                SavedByUsers = new List<SavedRecipe>
            {
                new SavedRecipe { UserId = "u1" }
            }
            };

            _context.RecipeCategories.Add(category);
            _context.Users.Add(new ApplicationUser { Id = "u1", UserName = "user1" });
            _context.Recipies.Add(recipe);

            await _context.SaveChangesAsync();

            var result = await _service.GetRecipeStatisticsAsync();

            Assert.Single(result.MostCommentedRecipes);
            Assert.Single(result.MostSavedRecipes);
            Assert.True(result.CategoryDistribution.ContainsKey("Category A"));
            Assert.True(result.HourDistribution.ContainsKey("10:00"));
        }
    }
}
