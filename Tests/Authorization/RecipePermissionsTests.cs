using System.Security.Claims;
using RecipeBook.Authorization;
using RecipeBook.Constants;

namespace Tests.Authorization
{
    public class RecipePermissionsTests
    {
        [Fact]
        public void Owner_CanModify()
        {
            Assert.True(RecipePermissions.CanModify("author", CreateUser("author")));
        }

        [Fact]
        public void Admin_CanModifyAnyRecipe()
        {
            Assert.True(RecipePermissions.CanModify("author", CreateUser("admin", UserRoles.Admin)));
        }

        [Fact]
        public void OtherUser_CannotModify()
        {
            Assert.False(RecipePermissions.CanModify("author", CreateUser("stranger", UserRoles.User)));
        }

        [Fact]
        public void AnonymousUser_CannotModify()
        {
            Assert.False(RecipePermissions.CanModify("author", new ClaimsPrincipal(new ClaimsIdentity())));
        }

        [Fact]
        public void AnonymousUser_CannotModifyRecipeWithoutOwner()
        {
            Assert.False(RecipePermissions.CanModify(null, new ClaimsPrincipal(new ClaimsIdentity())));
        }

        private static ClaimsPrincipal CreateUser(string userId, string? role = null)
        {
            var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, userId) };
            if (role != null)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            return new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));
        }
    }
}
