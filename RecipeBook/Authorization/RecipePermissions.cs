using System.Security.Claims;
using RecipeBook.Constants;

namespace RecipeBook.Authorization
{
    /// <summary>
    /// Ownership rules for recipes: only the author or an administrator may change or delete a recipe.
    /// </summary>
    public static class RecipePermissions
    {
        public static bool CanModify(string? recipeOwnerId, ClaimsPrincipal user)
        {
            if (user.IsInRole(UserRoles.Admin))
            {
                return true;
            }

            var currentUserId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            return !string.IsNullOrEmpty(currentUserId) && recipeOwnerId == currentUserId;
        }
    }
}
