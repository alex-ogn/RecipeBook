namespace RecipeBook.Views.Recipes.ViewModels
{
    public class RecipeCardViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string DescriptionPreview { get; set; }
        public string ImageUrl { get; set; }
        public string UserName { get; set; }
        public string UserId { get; set; }
        public int LikesCount { get; set; }
        public int ViewCount { get; set; }
        public bool IsOwner { get; set; }
    }
}
