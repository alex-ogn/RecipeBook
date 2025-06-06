namespace RecipeBook.ViewModels.AdminStatistics
{
    public class RecipeAnalyticsViewModel
    {
        public List<RecipeStatEntry> MostCommentedRecipes { get; set; }
        public List<RecipeStatEntry> MostSavedRecipes { get; set; }
        public Dictionary<string, int> CategoryDistribution { get; set; } = new();
        public Dictionary<string, int> HourDistribution { get; set; } = new();
    }
}
