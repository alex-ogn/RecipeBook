using System.ComponentModel.DataAnnotations;
using RecipeBook.Constants;
using RecipeBook.Resources;

namespace RecipeBook.Services
{
    /// <summary>
    /// Stores recipe images on disk. Only files that look like JPG, PNG or WEBP images are accepted,
    /// and deletes are restricted to the recipe images folder.
    /// </summary>
    public class ImageStorageService : IImageStorageService
    {
        private static readonly byte[] JpegSignature = { 0xFF, 0xD8, 0xFF };
        private static readonly byte[] PngSignature = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
        private static readonly byte[] RiffSignature = "RIFF"u8.ToArray();
        private static readonly byte[] WebpSignature = "WEBP"u8.ToArray();
        private const int WebpSignatureOffset = 8;
        private const int HeaderLength = 12;

        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<ImageStorageService> _logger;

        public ImageStorageService(IWebHostEnvironment environment, ILogger<ImageStorageService> logger)
        {
            _environment = environment;
            _logger = logger;
        }

        public string? Validate(IFormFile? file)
        {
            if (file == null || file.Length == 0)
            {
                return ValidationTexts.InvalidImageType;
            }

            if (file.Length > ImageConstants.MaxImageSizeBytes)
            {
                return string.Format(ValidationTexts.ImageTooLarge, ImageConstants.MaxImageSizeMegabytes);
            }

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!ImageConstants.AllowedImageExtensions.Contains(extension) || !HasImageSignature(file))
            {
                return ValidationTexts.InvalidImageType;
            }

            return null;
        }

        public async Task<string> SaveRecipeImageAsync(IFormFile file)
        {
            var error = Validate(file);
            if (error != null)
            {
                throw new ValidationException(error);
            }

            var folder = GetRecipeImagesFolder();
            Directory.CreateDirectory(folder);

            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName).ToLowerInvariant()}";
            await using (var stream = new FileStream(Path.Combine(folder, fileName), FileMode.CreateNew))
            {
                await file.CopyToAsync(stream);
            }

            return ImageConstants.RecipeImagesUrlPrefix + fileName;
        }

        public void DeleteRecipeImage(string? imageUrl)
        {
            var path = ResolveRecipeImagePath(imageUrl);
            if (path == null || !File.Exists(path))
            {
                return;
            }

            try
            {
                File.Delete(path);
            }
            catch (IOException ex)
            {
                _logger.LogWarning(ex, "Could not delete recipe image {ImageUrl}", imageUrl);
            }
        }

        /// <summary>
        /// Maps a recipe image URL to a file path inside the recipe images folder, or null when the URL
        /// points anywhere else (other folders, "..", nested paths).
        /// </summary>
        private string? ResolveRecipeImagePath(string? imageUrl)
        {
            if (string.IsNullOrEmpty(imageUrl)
                || !imageUrl.StartsWith(ImageConstants.RecipeImagesUrlPrefix, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            var fileName = imageUrl.Substring(ImageConstants.RecipeImagesUrlPrefix.Length);
            if (fileName.Length == 0
                || fileName != Path.GetFileName(fileName)
                || fileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0
                || fileName.Contains(".."))
            {
                return null;
            }

            var folder = GetRecipeImagesFolder();
            var fullPath = Path.GetFullPath(Path.Combine(folder, fileName));
            return fullPath.StartsWith(folder + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
                ? fullPath
                : null;
        }

        private string GetRecipeImagesFolder()
            => Path.GetFullPath(Path.Combine(_environment.WebRootPath, ImageConstants.ImagesFolder, ImageConstants.RecipeImagesFolder));

        private static bool HasImageSignature(IFormFile file)
        {
            var header = new byte[HeaderLength];
            int read;
            using (var stream = file.OpenReadStream())
            {
                read = stream.ReadAtLeast(header, HeaderLength, throwOnEndOfStream: false);
            }

            var bytes = header.AsSpan(0, read);
            return bytes.StartsWith(JpegSignature)
                || bytes.StartsWith(PngSignature)
                || (read == HeaderLength
                    && bytes.StartsWith(RiffSignature)
                    && bytes.Slice(WebpSignatureOffset, WebpSignature.Length).SequenceEqual(WebpSignature));
        }
    }
}
