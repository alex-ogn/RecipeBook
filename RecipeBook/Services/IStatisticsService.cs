using RecipeBook.ViewModels.AdminStatistics;

namespace RecipeBook.Services
{
    public interface IStatisticsService
    {
        Task<AdminUserStatsViewModel> GetUserStatisticsAsync();
        Task<RecipeAnalyticsViewModel> GetRecipeStatsAsync();
    }
}
