namespace RecipeBook.Services
{
    /// <summary>
    /// Validates uploaded images and stores recipe images and profile pictures under wwwroot.
    /// </summary>
    public interface IImageStorageService
    {
        /// <summary>
        /// Checks extension, size and file signature.
        /// </summary>
        /// <returns>A user-facing error message, or null when the file is a valid image.</returns>
        string? Validate(IFormFile? file);

        /// <summary>
        /// Validates and saves a recipe image.
        /// </summary>
        /// <returns>The public URL of the saved image.</returns>
        /// <exception cref="System.ComponentModel.DataAnnotations.ValidationException">The file is not a valid image.</exception>
        Task<string> SaveRecipeImageAsync(IFormFile file);

        /// <summary>
        /// Deletes a recipe image. URLs outside the recipe images folder are ignored.
        /// </summary>
        void DeleteRecipeImage(string? imageUrl);

        /// <summary>
        /// Validates and saves a profile picture.
        /// </summary>
        /// <returns>The public URL of the saved picture.</returns>
        /// <exception cref="System.ComponentModel.DataAnnotations.ValidationException">The file is not a valid image.</exception>
        Task<string> SaveProfilePictureAsync(IFormFile file);

        /// <summary>
        /// Deletes a profile picture. URLs outside the profile pictures folder are ignored.
        /// </summary>
        void DeleteProfilePicture(string? imageUrl);
    }
}
