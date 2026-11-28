using System.ComponentModel.DataAnnotations;
using RecipeBook.Constants;
using RecipeBook.Resources;

namespace RecipeBook.Services
{
    /// <summary>
    /// Stores recipe images and profile pictures on disk. Only files that look like JPG, PNG or WEBP images
    /// are accepted, and deletes are restricted to the folder the image belongs to.
    /// </summary>
    public class ImageStorageService : IImageStorageService
    {
        private static readonly byte[] JpegSignature = { 0xFF, 0xD8, 0xFF };
        private static readonly byte[] PngSignature = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
        private static readonly byte[] RiffSignature = "RIFF"u8.ToArray();
        private static readonly byte[] WebpSignature = "WEBP"u8.ToArray();
        private const int WebpSignatureOffset = 8;
        private const int HeaderLength = 12;
        private const string JpegExtension = ".jpg";
        private const string PngExtension = ".png";
        private const string WebpExtension = ".webp";

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

        public Task<string> SaveRecipeImageAsync(IFormFile file)
            => SaveAsync(file, ImageConstants.RecipeImagesFolder, ImageConstants.RecipeImagesUrlPrefix);

        public void DeleteRecipeImage(string? imageUrl)
            => Delete(imageUrl, ImageConstants.RecipeImagesFolder, ImageConstants.RecipeImagesUrlPrefix);

        public Task<string> SaveProfilePictureAsync(IFormFile file)
            => SaveAsync(file, ImageConstants.ProfileImagesFolder, ImageConstants.ProfileImagesUrlPrefix);

        public void DeleteProfilePicture(string? imageUrl)
            => Delete(imageUrl, ImageConstants.ProfileImagesFolder, ImageConstants.ProfileImagesUrlPrefix);

        private async Task<string> SaveAsync(IFormFile file, string folderName, string urlPrefix)
        {
            var error = Validate(file);
            if (error != null)
            {
                throw new ValidationException(error);
            }

            var (path, url) = NewFile(folderName, urlPrefix, Path.GetExtension(file.FileName).ToLowerInvariant());
            await using (var stream = new FileStream(path, FileMode.CreateNew))
            {
                await file.CopyToAsync(stream);
            }

            return url;
        }

        /// <summary>
        /// A new random file name in the folder (created if missing): its full path and public URL.
        /// </summary>
        private (string Path, string Url) NewFile(string folderName, string urlPrefix, string extension)
        {
            var folder = GetFolder(folderName);
            Directory.CreateDirectory(folder);

            var fileName = $"{Guid.NewGuid()}{extension}";
            return (Path.Combine(folder, fileName), urlPrefix + fileName);
        }

        private void Delete(string? imageUrl, string folderName, string urlPrefix)
        {
            var path = ResolvePath(imageUrl, folderName, urlPrefix);
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
                _logger.LogWarning(ex, "Could not delete image {ImageUrl}", imageUrl);
            }
        }

        /// <summary>
        /// Maps an image URL to a file path inside the given images folder, or null when the URL
        /// points anywhere else (other folders, "..", nested paths).
        /// </summary>
        private string? ResolvePath(string? imageUrl, string folderName, string urlPrefix)
        {
            if (string.IsNullOrEmpty(imageUrl)
                || !imageUrl.StartsWith(urlPrefix, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            var fileName = imageUrl.Substring(urlPrefix.Length);
            if (fileName.Length == 0
                || fileName != Path.GetFileName(fileName)
                || fileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0
                || fileName.Contains(".."))
            {
                return null;
            }

            var folder = GetFolder(folderName);
            var fullPath = Path.GetFullPath(Path.Combine(folder, fileName));
            return fullPath.StartsWith(folder + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
                ? fullPath
                : null;
        }

        private string GetFolder(string folderName)
            => Path.GetFullPath(Path.Combine(_environment.WebRootPath, ImageConstants.ImagesFolder, folderName));

        private static bool HasImageSignature(IFormFile file)
        {
            var header = new byte[HeaderLength];
            int read;
            using (var stream = file.OpenReadStream())
            {
                read = stream.ReadAtLeast(header, HeaderLength, throwOnEndOfStream: false);
            }

            return GetImageExtension(header.AsSpan(0, read)) != null;
        }

        /// <summary>
        /// The file extension matching the image signature at the start of the bytes, or null when it is not
        /// a JPG, PNG or WEBP image.
        /// </summary>
        private static string? GetImageExtension(ReadOnlySpan<byte> bytes)
        {
            if (bytes.StartsWith(JpegSignature)) return JpegExtension;
            if (bytes.StartsWith(PngSignature)) return PngExtension;
            if (bytes.Length >= HeaderLength
                && bytes.StartsWith(RiffSignature)
                && bytes.Slice(WebpSignatureOffset, WebpSignature.Length).SequenceEqual(WebpSignature))
            {
                return WebpExtension;
            }

            return null;
        }
    }
}
