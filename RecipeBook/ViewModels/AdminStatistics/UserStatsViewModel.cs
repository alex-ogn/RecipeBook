namespace RecipeBook.ViewModels.AdminStatistics
{
    public class UserStatsViewModel
    {
        public string UserName { get; set; }
        public int RecipeCount { get; set; }
        public int LikesReceived { get; set; }
        public bool HasSaved { get; set; }
        public bool HasLiked { get; set; }
    }

}
