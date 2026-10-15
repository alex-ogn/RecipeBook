using Microsoft.EntityFrameworkCore;
using RecipeBook.Data;
using RecipeBook.ViewModels.AdminStatistics;

namespace RecipeBook.Services
{
    /// <summary>
    /// Class for recipes and user statistics 
    /// </summary>
    public class StatisticsService : IStatisticsService
    {
        private readonly ApplicationDbContext _context;

        public StatisticsService(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Return view model with user statistic
        /// </summary>
        /// <returns></returns>
        public async Task<AdminUserStatsViewModel> GetUserStatisticsAsync()
        {
            // One query with the counts computed in the database, instead of loading every user's recipes and likes.
            var users = await _context.Users
                .AsNoTracking()
                .OrderBy(u => u.UserName)
                .Select(u => new UserStatsViewModel
                {
                    UserName = u.UserName ?? string.Empty,
                    RecipeCount = u.Recipes.Count,
                    // A plain COUNT; SQL Server cannot SUM over a COUNT subquery.
                    LikesReceived = _context.RecipeLikes.Count(l => l.Recipe.UserId == u.Id),
                    HasSaved = u.SavedRecipes.Any(),
                    HasLiked = u.LikedRecipes.Any()
                })
                .ToListAsync();

            var inactiveUsers = users
                .Where(u => u.RecipeCount == 0 && !u.HasSaved && !u.HasLiked)
                .ToList();

            return new AdminUserStatsViewModel
            {
                AllUsers = users,
                InactiveUsers = inactiveUsers
            };
        }

        /// <summary>
        /// Return view model with recipe statistic
        /// </summary>
        /// <returns></returns>
        public async Task<RecipeAnalyticsViewModel> GetRecipeStatisticsAsync()
        {
            var mostCommented = await _context.Recipes
                .OrderByDescending(r => r.Comments.Count)
                .Take(5)
                .Select(r => new RecipeStatEntry
                {
                    Id= r.Id,
                    Title = r.Title,
                    Count = r.Comments.Count
                })
                .ToListAsync();

            var mostSaved = await _context.Recipes
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

            // Grouped in the database; only 24 rows come back instead of every recipe.
            var hourlyCounts = await _context.Recipes
                .GroupBy(r => r.CreatedAt.Hour)
                .Select(g => new { Hour = g.Key, Count = g.Count() })
                .ToListAsync();

            var hourlyDistribution = hourlyCounts
                .OrderBy(h => h.Hour)
                .ToDictionary(h => h.Hour.ToString("00") + ":00", h => h.Count);

            var categoryMap = await _context.RecipeCategories
                 .AsNoTracking()
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
