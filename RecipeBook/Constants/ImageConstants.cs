namespace RecipeBook.Constants
{
    public static class ImageConstants
    {
        // Folders under wwwroot
        public const string ImagesFolder = "images";
        public const string RecipeImagesFolder = "recipes";
        public const string ProfileImagesFolder = "profiles";

        // Public URLs
        public const string RecipeImagesUrlPrefix = "/" + ImagesFolder + "/" + RecipeImagesFolder + "/";
        public const string ProfileImagesUrlPrefix = "/" + ImagesFolder + "/" + ProfileImagesFolder + "/";
        public const string DefaultRecipeImageUrl = "/" + ImagesFolder + "/default-recipe.jpg";
        public const string DefaultProfilePictureUrl = "/" + ImagesFolder + "/" + UserProfileConstants.DefaultProfilePictureName;

        // Stored image URLs (Recipe.ImageUrl, ApplicationUser.ProfilePictureUrl)
        public const int MaxImageUrlLength = 500;

        // Uploaded images get a new GUID file name on every upload, so browsers may cache them for good.
        public const int UploadedImageCacheSeconds = 365 * 24 * 60 * 60;

        // Upload limits
        public const int MaxImageSizeMegabytes = 5;
        public const long MaxImageSizeBytes = MaxImageSizeMegabytes * 1024L * 1024L;
        public static readonly string[] AllowedImageExtensions = { ".jpg", ".jpeg", ".png", ".webp" };

        /// <summary>
        /// The stored profile picture URL, or the default avatar when the user has none.
        /// </summary>
        public static string ProfilePictureOrDefault(string? profilePictureUrl)
            => string.IsNullOrWhiteSpace(profilePictureUrl) ? DefaultProfilePictureUrl : profilePictureUrl;
    }
}
