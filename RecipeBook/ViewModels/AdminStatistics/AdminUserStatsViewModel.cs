namespace RecipeBook.ViewModels.AdminStatistics
{
    public class AdminUserStatsViewModel
    {
        public List<UserStatsViewModel> AllUsers { get; set; } = new List<UserStatsViewModel>();
        public List<UserStatsViewModel> InactiveUsers { get; set; } = new List<UserStatsViewModel>();
    }
}
