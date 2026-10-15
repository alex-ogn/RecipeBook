namespace RecipeBook.ViewModels.AdminStatistics
{
    public class RecipeAnalyticsViewModel
    {
        public List<RecipeStatEntry> MostCommentedRecipes { get; set; } = new();
        public List<RecipeStatEntry> MostSavedRecipes { get; set; } = new();
        public Dictionary<string, int> CategoryDistribution { get; set; } = new();
        public Dictionary<string, int> HourDistribution { get; set; } = new();
        public Dictionary<string, int> CategoryNameToIdMap { get; set; } = new();
    }
}
