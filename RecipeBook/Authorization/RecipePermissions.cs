using System.Security.Claims;
using RecipeBook.Constants;

namespace RecipeBook.Authorization
{
    /// <summary>
    /// Ownership rules for recipes and their comments.
    /// </summary>
    public static class RecipePermissions
    {
        /// <summary>
        /// Only the author or an administrator may change or delete a recipe.
        /// </summary>
        public static bool CanModify(string? recipeOwnerId, ClaimsPrincipal user)
            => IsAdmin(user) || IsCurrentUser(recipeOwnerId, user);

        /// <summary>
        /// Only the comment's author or an administrator may edit a comment.
        /// </summary>
        public static bool CanEditComment(string? commentAuthorId, ClaimsPrincipal user)
            => IsAdmin(user) || IsCurrentUser(commentAuthorId, user);

        /// <summary>
        /// The comment's author, the recipe's author or an administrator may delete a comment.
        /// </summary>
        public static bool CanDeleteComment(string? commentAuthorId, string? recipeOwnerId, ClaimsPrincipal user)
            => IsAdmin(user) || IsCurrentUser(commentAuthorId, user) || IsCurrentUser(recipeOwnerId, user);

        private static bool IsAdmin(ClaimsPrincipal user) => user.IsInRole(UserRoles.Admin);

        private static bool IsCurrentUser(string? userId, ClaimsPrincipal user)
        {
            var currentUserId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            return !string.IsNullOrEmpty(currentUserId) && userId == currentUserId;
        }
    }
}
