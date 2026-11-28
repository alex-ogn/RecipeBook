
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using RecipeBook.Constants;
using RecipeBook.Data;
using RecipeBook.Models;
using RecipeBook.Resources;
using RecipeBook.Services;
using RecipeBook.ViewModels.Users;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Tests.Services
{
    public class UserProfileServiceTests
    {
        private readonly Mock<UserManager<ApplicationUser>> _mockUserManager;
        private readonly Mock<IRecipeService> _mockRecipeService;
        private readonly Mock<IImageStorageService> _mockImageStorage;
        private readonly UserProfileService _service;

        public UserProfileServiceTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase("TestDb")
                .Options;

            var context = new ApplicationDbContext(options);

            _mockUserManager = MockUserManager();
            _mockRecipeService = new Mock<IRecipeService>();
            _mockImageStorage = new Mock<IImageStorageService>();

            var localizationOptions = Options.Create(new LocalizationOptions
            {
                ResourcesPath = "Resources"
            });

            var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            var factory = new ResourceManagerStringLocalizerFactory(localizationOptions, loggerFactory);

            _service = new UserProfileService(
                context,
                _mockUserManager.Object,
                _mockRecipeService.Object,
                _mockImageStorage.Object,
                factory
            );
        }

        private Mock<UserManager<ApplicationUser>> MockUserManager()
        {
            var store = new Mock<IUserStore<ApplicationUser>>();
            return new Mock<UserManager<ApplicationUser>>(store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        }

        [Fact]
        public async Task UpdateUserProfileAsync_ShouldFail_WhenUserNameExists()
        {
            var user = new ApplicationUser { Id = "1", UserName = "oldUser" };
            var model = new EditUserViewModel { UserName = "newUser", Email = "test@example.com" };

            _mockUserManager.Setup(m => m.FindByNameAsync("newUser"))
                .ReturnsAsync(new ApplicationUser { Id = "2", UserName = "newUser" });

            var result = await _service.UpdateUserProfileAsync(user, model);

            Assert.False(result.Succeeded);
        }

        [Fact]
        public async Task DeleteUserAsync_ShouldFail_WhenSameUser()
        {
            var result = await _service.DeleteUserAsync("123", "123");
            Assert.False(result.Succeeded);
        }

        [Fact]
        public async Task DeleteOwnAccountAsync_ShouldFail_ForAdmin()
        {
            var admin = new ApplicationUser { Id = "admin-id", UserName = "admin" };
            _mockUserManager.Setup(m => m.FindByIdAsync(admin.Id)).ReturnsAsync(admin);
            _mockUserManager.Setup(m => m.IsInRoleAsync(admin, UserRoles.Admin)).ReturnsAsync(true);

            var result = await _service.DeleteOwnAccountAsync(admin.Id);

            Assert.False(result.Succeeded);
            Assert.Contains(result.Errors, e => e.Description == MessageTexts.AdminCannotDeleteOwnAccount);
            _mockUserManager.Verify(m => m.DeleteAsync(It.IsAny<ApplicationUser>()), Times.Never);
        }

        [Fact]
        public async Task DeleteOwnAccountAsync_ShouldFail_WhenUserNotFound()
        {
            _mockUserManager.Setup(m => m.FindByIdAsync("missing")).ReturnsAsync((ApplicationUser?)null);

            var result = await _service.DeleteOwnAccountAsync("missing");

            Assert.False(result.Succeeded);
            _mockUserManager.Verify(m => m.DeleteAsync(It.IsAny<ApplicationUser>()), Times.Never);
        }

        private const string OldPictureUrl = ImageConstants.ProfileImagesUrlPrefix + "old.png";
        private const string NewPictureUrl = ImageConstants.ProfileImagesUrlPrefix + "new.png";

        private (ApplicationUser User, EditUserViewModel Model) UserUploadingPicture()
        {
            var user = new ApplicationUser { Id = "1", UserName = "user", ProfilePictureUrl = OldPictureUrl };
            var picture = new FormFile(new MemoryStream(new byte[] { 1 }), 0, 1, "ProfilePicture", "photo.png");
            var model = new EditUserViewModel { UserName = "user", ProfilePicture = picture };

            _mockImageStorage.Setup(s => s.Validate(picture)).Returns((string?)null);
            _mockImageStorage.Setup(s => s.SaveProfilePictureAsync(picture)).ReturnsAsync(NewPictureUrl);
            return (user, model);
        }

        [Fact]
        public async Task UpdateUserProfileAsync_SavesNewPicture_AndDeletesOldFile()
        {
            var (user, model) = UserUploadingPicture();
            _mockUserManager.Setup(m => m.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);

            var result = await _service.UpdateUserProfileAsync(user, model);

            Assert.True(result.Succeeded);
            Assert.Equal(NewPictureUrl, user.ProfilePictureUrl);
            _mockImageStorage.Verify(s => s.DeleteProfilePicture(OldPictureUrl), Times.Once);
            _mockImageStorage.Verify(s => s.DeleteProfilePicture(NewPictureUrl), Times.Never);
        }

        [Fact]
        public async Task UpdateUserProfileAsync_KeepsOldPicture_WhenSavingUserFails()
        {
            var (user, model) = UserUploadingPicture();
            _mockUserManager.Setup(m => m.UpdateAsync(user)).ReturnsAsync(IdentityResult.Failed(new IdentityError()));

            var result = await _service.UpdateUserProfileAsync(user, model);

            Assert.False(result.Succeeded);
            Assert.Equal(OldPictureUrl, user.ProfilePictureUrl);
            _mockImageStorage.Verify(s => s.DeleteProfilePicture(NewPictureUrl), Times.Once);
            _mockImageStorage.Verify(s => s.DeleteProfilePicture(OldPictureUrl), Times.Never);
        }

        [Fact]
        public async Task UpdateUserProfileAsync_DoesNotSave_WhenPictureIsInvalid()
        {
            var (user, model) = UserUploadingPicture();
            _mockImageStorage.Setup(s => s.Validate(model.ProfilePicture)).Returns(ValidationTexts.InvalidImageType);

            var result = await _service.UpdateUserProfileAsync(user, model);

            Assert.False(result.Succeeded);
            Assert.Equal(OldPictureUrl, user.ProfilePictureUrl);
            _mockImageStorage.Verify(s => s.SaveProfilePictureAsync(It.IsAny<IFormFile>()), Times.Never);
            _mockUserManager.Verify(m => m.UpdateAsync(It.IsAny<ApplicationUser>()), Times.Never);
        }
    }
}
