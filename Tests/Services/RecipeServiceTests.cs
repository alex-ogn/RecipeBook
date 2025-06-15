using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Moq;
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
    public class RecipeServiceTests
    {
        private readonly RecipeService _service;
        private readonly ApplicationDbContext _context;

        public RecipeServiceTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: System.Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);
            var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();

            _service = new RecipeService(_context, mockHttpContextAccessor.Object);
        }

        private Recipe CreateValidRecipe(int id, string userId)
        {
            return new Recipe
            {
                Id = id,
                UserId = userId,
                Title = "Test Recipe",
                Description = "A valid test recipe",
                Instructions = "Step 1: Do this. Step 2: Do that.",
                Servings = 2,
                CookingTime = 30,
                RecipeCategoryId = 1,
                CreatedAt = System.DateTime.UtcNow
            };
        }

        [Fact]
        public async Task DeleteRecipeAsync_ShouldReturnFalse_WhenRecipeNotFound()
        {
            var result = await _service.DeleteRecipeAsync(999);
            Assert.False(result);
        }

        [Fact]
        public async Task DeleteRecipeAsync_ShouldReturnFalse_WhenUserNotOwnerAndNotForced()
        {
            var recipe = CreateValidRecipe(101, "user1");
            _context.Recipies.Add(recipe);
            await _context.SaveChangesAsync();

            var result = await _service.DeleteRecipeAsync(101, "user2", forceDelete: false);
            Assert.False(result);
        }

        [Fact]
        public async Task DeleteRecipeAsync_ShouldReturnTrue_WhenForced()
        {
            var recipe = CreateValidRecipe(102, "user1");
            _context.Recipies.Add(recipe);
            await _context.SaveChangesAsync();

            var result = await _service.DeleteRecipeAsync(102, "user2", forceDelete: true);
            Assert.True(result);
        }

        [Fact]
        public async Task GetSimilarRecipesAsync_ShouldReturnSimilarRecipes()
        {
            _context.Users.AddRange(
                new ApplicationUser { Id = "u1", UserName = "User One" },
                new ApplicationUser { Id = "u2", UserName = "User Two" }
            );
            await _context.SaveChangesAsync();

            var baseRecipe = CreateValidRecipe(201, "u1");
            baseRecipe.Title = "Test";
            baseRecipe.IsVegan = true;

            var similar = CreateValidRecipe(202, "u1");
            similar.Title = "Test Vegan";
            similar.IsVegan = true;

            var other = CreateValidRecipe(203, "u2");
            other.Title = "Unrelated";
            other.IsVegan = false;
            other.RecipeCategoryId = 99;

            _context.Recipies.AddRange(baseRecipe, similar, other);
            await _context.SaveChangesAsync();

            var result = await _service.GetSimilarRecipesAsync(baseRecipe, "u1");

            Assert.Contains(result, r => r.Title == "Test Vegan");
        }

        [Fact]
        public async Task GetSuggestedRecipesForUserAsync_ShouldReturnSuggestions()
        {
            _context.Users.AddRange(
                new ApplicationUser { Id = "user1", UserName = "User One" },
                new ApplicationUser { Id = "user2", UserName = "User Two" }
            );
            await _context.SaveChangesAsync();

            var reference = CreateValidRecipe(301, "user1");
            reference.Title = "Pasta";
            reference.IsVegan = true;

            var candidate = CreateValidRecipe(302, "user2");
            candidate.Title = "Vegan Pasta";
            candidate.IsVegan = true;

            _context.Recipies.AddRange(reference, candidate);
            _context.SavedRecipes.Add(new SavedRecipe { UserId = "user1", RecipeId = 301 });
            await _context.SaveChangesAsync();

            var result = await _service.GetSuggestedRecipesForUserAsync("user1");

            Assert.Single(result);
            Assert.Equal("Vegan Pasta", result[0].Title);
        }
    }

}
