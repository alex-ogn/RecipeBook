using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using RecipeBook.Constants;
using RecipeBook.Resources;
using RecipeBook.Services;

namespace Tests.Services
{
    public class ImageStorageServiceTests : IDisposable
    {
        private static readonly byte[] PngHeader = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0, 0, 0, 0 };
        private static readonly byte[] JpegHeader = { 0xFF, 0xD8, 0xFF, 0xE0, 0, 0, 0, 0, 0, 0, 0, 0 };
        private static readonly byte[] WebpHeader = "RIFF\0\0\0\0WEBP"u8.ToArray();

        private readonly string _webRoot;
        private readonly string _recipeImagesFolder;
        private readonly ImageStorageService _service;

        public ImageStorageServiceTests()
        {
            _webRoot = Path.Combine(Path.GetTempPath(), "RecipeBookTests", Guid.NewGuid().ToString());
            _recipeImagesFolder = Path.Combine(_webRoot, ImageConstants.ImagesFolder, ImageConstants.RecipeImagesFolder);
            Directory.CreateDirectory(_recipeImagesFolder);

            var env = new Mock<IWebHostEnvironment>();
            env.Setup(e => e.WebRootPath).Returns(_webRoot);
            _service = new ImageStorageService(env.Object, NullLogger<ImageStorageService>.Instance);
        }

        public void Dispose() => Directory.Delete(_webRoot, recursive: true);

        [Theory]
        [InlineData("photo.png")]
        [InlineData("photo.JPG")]
        [InlineData("photo.jpeg")]
        [InlineData("photo.webp")]
        public void Validate_AcceptsAllowedImages(string fileName)
        {
            var header = Path.GetExtension(fileName).ToLowerInvariant() switch
            {
                ".png" => PngHeader,
                ".webp" => WebpHeader,
                _ => JpegHeader
            };

            Assert.Null(_service.Validate(CreateFile(fileName, header)));
        }

        [Theory]
        [InlineData("page.html")]
        [InlineData("image.svg")]
        [InlineData("script.png.exe")]
        public void Validate_RejectsDisallowedExtensions(string fileName)
        {
            Assert.Equal(ValidationTexts.InvalidImageType, _service.Validate(CreateFile(fileName, PngHeader)));
        }

        [Fact]
        public void Validate_RejectsFileWhoseContentIsNotAnImage()
        {
            var html = "<html><script>alert(1)</script></html>"u8.ToArray();

            Assert.Equal(ValidationTexts.InvalidImageType, _service.Validate(CreateFile("evil.png", html)));
        }

        [Fact]
        public void Validate_RejectsFilesOverTheSizeLimit()
        {
            var file = CreateFile("big.png", PngHeader, length: ImageConstants.MaxImageSizeBytes + 1);

            Assert.Equal(string.Format(ValidationTexts.ImageTooLarge, ImageConstants.MaxImageSizeMegabytes), _service.Validate(file));
        }

        [Fact]
        public async Task SaveRecipeImageAsync_StoresFileUnderRecipeFolder()
        {
            var url = await _service.SaveRecipeImageAsync(CreateFile("photo.png", PngHeader));

            Assert.StartsWith(ImageConstants.RecipeImagesUrlPrefix, url);
            Assert.True(File.Exists(Path.Combine(_recipeImagesFolder, Path.GetFileName(url))));
        }

        [Fact]
        public async Task SaveRecipeImageAsync_ThrowsForInvalidFile()
        {
            await Assert.ThrowsAsync<ValidationException>(() => _service.SaveRecipeImageAsync(CreateFile("page.html", PngHeader)));
        }

        [Fact]
        public void DeleteRecipeImage_DeletesFileInRecipeFolder()
        {
            var path = Path.Combine(_recipeImagesFolder, "photo.jpg");
            File.WriteAllBytes(path, JpegHeader);

            _service.DeleteRecipeImage(ImageConstants.RecipeImagesUrlPrefix + "photo.jpg");

            Assert.False(File.Exists(path));
        }

        [Theory]
        [InlineData("/images/recipes/../../secret.txt")]
        [InlineData("/images/recipes/..\\..\\secret.txt")]
        [InlineData("../secret.txt")]
        [InlineData("/secret.txt")]
        [InlineData("/images/secret.txt")]
        public void DeleteRecipeImage_IgnoresPathsOutsideRecipeFolder(string imageUrl)
        {
            var secret = Path.Combine(_webRoot, "secret.txt");
            File.WriteAllText(secret, "keep me");
            File.WriteAllText(Path.Combine(_webRoot, ImageConstants.ImagesFolder, "secret.txt"), "keep me");

            _service.DeleteRecipeImage(imageUrl);

            Assert.True(File.Exists(secret));
            Assert.True(File.Exists(Path.Combine(_webRoot, ImageConstants.ImagesFolder, "secret.txt")));
        }

        private static IFormFile CreateFile(string fileName, byte[] content, long? length = null)
        {
            var stream = new MemoryStream(content);
            return new FormFile(stream, 0, length ?? content.Length, "file", fileName);
        }
    }
}
