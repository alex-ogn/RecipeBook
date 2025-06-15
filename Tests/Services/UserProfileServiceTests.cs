
using Xunit;
using Moq;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RecipeBook.Data;
using RecipeBook.Models;
using RecipeBook.Services;
using RecipeBook.ViewModels.Users;
using System.Threading.Tasks;
using System.IO;
using Microsoft.AspNetCore.Hosting;
using System.Collections.Generic;
using System.Linq;

namespace Tests.Services
{
    public class UserProfileServiceTests
    {
        private readonly Mock<ApplicationDbContext> _mockContext;
        private readonly Mock<UserManager<ApplicationUser>> _mockUserManager;
        private readonly Mock<IPasswordValidator<ApplicationUser>> _mockPasswordValidator;
        private readonly Mock<IPasswordHasher<ApplicationUser>> _mockPasswordHasher;
        private readonly Mock<IRecipeService> _mockRecipeService;
        private readonly Mock<IWebHostEnvironment> _mockEnv;
        private readonly UserProfileService _service;

        public UserProfileServiceTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase("TestDb")
                .Options;

            var context = new ApplicationDbContext(options);

            _mockUserManager = MockUserManager();
            _mockPasswordValidator = new Mock<IPasswordValidator<ApplicationUser>>();
            _mockPasswordHasher = new Mock<IPasswordHasher<ApplicationUser>>();
            _mockRecipeService = new Mock<IRecipeService>();
            _mockEnv = new Mock<IWebHostEnvironment>();

            var solutionRoot = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "../../../../.."));
            var realWebRoot = Path.Combine(solutionRoot, "RecipeBook", "RecipeBook", "wwwroot");

            _mockEnv.Setup(e => e.WebRootPath).Returns(realWebRoot);

            _service = new UserProfileService(
                context,
                _mockUserManager.Object,
                _mockPasswordValidator.Object,
                _mockPasswordHasher.Object,
                _mockRecipeService.Object,
                _mockEnv.Object
            );
        }

        private Mock<UserManager<ApplicationUser>> MockUserManager()
        {
            var store = new Mock<IUserStore<ApplicationUser>>();
            return new Mock<UserManager<ApplicationUser>>(store.Object, null, null, null, null, null, null, null, null);
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
        public async Task GetProfilePictureAsync_ShouldReturnDefault_WhenNoPicture()
        {
            _mockUserManager.Setup(m => m.FindByIdAsync("non-existent-id"))
                .ReturnsAsync((ApplicationUser)null);

            var (content, contentType) = await _service.GetProfilePictureAsync("non-existent-id");

            Assert.NotNull(content);
            Assert.Equal("image/png", contentType);
        }
    }
}
