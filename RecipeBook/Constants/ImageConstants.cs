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
    }
}
