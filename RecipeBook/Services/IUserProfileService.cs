using Microsoft.AspNetCore.Identity;
using RecipeBook.Models;
using RecipeBook.ViewModels.Users;

namespace RecipeBook.Services
{
    /// <summary>
    /// Interface for managing user profiles
    /// </summary>
    public interface IUserProfileService
    {
        /// <summary>
        /// Updates user profile
        /// </summary>
        /// <param name="user"></param>
        /// <param name="model"></param>
        /// <param name="isAdmin"></param>
        /// <returns></returns>
        Task<IdentityResult> UpdateUserProfileAsync(ApplicationUser user, EditUserViewModel model, bool isAdmin = false);
        /// <summary>
        /// Deletes user
        /// </summary>
        /// <param name="targetUserId"></param>
        /// <param name="currentUserId"></param>
        /// <returns></returns>
        Task<IdentityResult> DeleteUserAsync(string targetUserId, string currentUserId);
        /// <summary>
        /// Return profile picture for user id
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        Task<(byte[] Content, string ContentType)> GetProfilePictureAsync(string userId);
    }
}
