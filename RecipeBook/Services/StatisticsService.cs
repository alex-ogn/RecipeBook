using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RecipeBook.Data;
using RecipeBook.ViewModels.AdminStatistics;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RecipeBook.Services
{
    public class StatisticsService : IStatisticsService
    {
        private readonly ApplicationDbContext _context;

        public StatisticsService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<AdminUserStatsViewModel> GetUserStatisticsAsync()
        {
            var users = _context.Users
                .Include(u => u.Recipes)
                    .ThenInclude(r => r.Likes)
                .Include(u => u.SavedRecipes)
                .Include(u => u.LikedRecipes)
                .ToList() 
                .Select(u => new UserStatsViewModel
                {
                    UserName = u.UserName,
                    RecipeCount = u.Recipes.Count,
                    LikesReceived = u.Recipes.Sum(r => r.Likes.Count),
                    HasSaved = u.SavedRecipes.Any(),
                    HasLiked = u.LikedRecipes.Any()
                })
                .ToList();

            var inactiveUsers = users
                .Where(u => u.RecipeCount == 0 && !u.HasSaved && !u.HasLiked)
                .ToList();

            return new AdminUserStatsViewModel
            {
                AllUsers = users,
                InactiveUsers = inactiveUsers
            };
        }

        public async Task<RecipeAnalyticsViewModel> GetRecipeStatisticsAsync()
        {
            var mostCommented = await _context.Recipies
                .OrderByDescending(r => r.Comments.Count)
                .Take(5)
                .Select(r => new RecipeStatEntry
                {
                    Id= r.Id,
                    Title = r.Title,
                    Count = r.Comments.Count
                })
                .ToListAsync();

            var mostSaved = await _context.Recipies
                .OrderByDescending(r => r.SavedByUsers.Count)
                .Take(5)
                .Select(r => new RecipeStatEntry
                {
                    Id = r.Id,
                    Title = r.Title,
                    Count = r.SavedByUsers.Count
                })
                .ToListAsync();

            var categoryDistribution = await _context.RecipeCategories
                .Select(c => new
                {
                    c.Name,
                    Count = c.Recipes.Count()
                })
                .ToDictionaryAsync(c => c.Name, c => c.Count);

            var hourlyDistribution = _context.Recipies
                .AsEnumerable() 
                .GroupBy(r => r.CreatedAt.Hour)
                .ToDictionary(
                    g => g.Key.ToString("00") + ":00",
                    g => g.Count()
                );

            var categoryMap = await _context.RecipeCategories
                 .ToDictionaryAsync(c => c.Name, c => c.Id);

            return new RecipeAnalyticsViewModel
            {
                MostCommentedRecipes = mostCommented,
                MostSavedRecipes = mostSaved,
                CategoryDistribution = categoryDistribution,
                HourDistribution = hourlyDistribution,
                CategoryNameToIdMap = categoryMap

            };
        }

    }

}
