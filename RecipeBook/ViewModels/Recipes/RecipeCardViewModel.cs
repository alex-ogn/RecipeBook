namespace RecipeBook.ViewModels.Recipes
{
    public class RecipeCardViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string DescriptionPreview { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public int LikesCount { get; set; }
        public int ViewCount { get; set; }
        public bool IsOwner { get; set; }
    }
}
