using RecipeBook.ViewModels.AdminStatistics;

namespace RecipeBook.Services
{
    /// <summary>
    /// Interface for creating statistics
    /// </summary>
    public interface IStatisticsService
    {
        /// <summary>
        /// Create user statistic
        /// </summary>
        /// <returns></returns>
        Task<AdminUserStatsViewModel> GetUserStatisticsAsync();
        /// <summary>
        /// Create recipe statistic
        /// </summary>
        /// <returns></returns>
        Task<RecipeAnalyticsViewModel> GetRecipeStatisticsAsync();
    }
}
