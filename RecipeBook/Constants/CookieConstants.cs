namespace RecipeBook.Constants
{
    public static class CookieConstants
    {
        // Marks a recipe as already viewed so repeated visits don't inflate its view count.
        public const string ViewedRecipePrefix = "viewed_recipe_";
        public static readonly TimeSpan ViewedRecipeLifetime = TimeSpan.FromHours(1);

        // Where the authentication cookie sends the browser (AccountController actions).
        public const string LoginPath = "/Account/Login";
        public const string LogoutPath = "/Account/Logout";
        public const string AccessDeniedPath = "/Account/AccessDenied";
    }
}
