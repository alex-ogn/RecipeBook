namespace RecipeBook.Constants
{
    public static class ImageConstants
    {
        // Folders under wwwroot
        public const string ImagesFolder = "images";
        public const string RecipeImagesFolder = "recipes";

        // Public URLs
        public const string RecipeImagesUrlPrefix = "/" + ImagesFolder + "/" + RecipeImagesFolder + "/";
        public const string DefaultRecipeImageUrl = "/" + ImagesFolder + "/default-recipe.jpg";

        // Upload limits
        public const int MaxImageSizeMegabytes = 5;
        public const long MaxImageSizeBytes = MaxImageSizeMegabytes * 1024L * 1024L;
        public static readonly string[] AllowedImageExtensions = { ".jpg", ".jpeg", ".png", ".webp" };
    }
}
