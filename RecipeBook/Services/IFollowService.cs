using RecipeBook.ViewModels.Users;

namespace RecipeBook.Services
{
    /// <summary>
    /// Following relationships between users.
    /// </summary>
    public interface IFollowService
    {
        Task<bool> IsFollowingAsync(string? followerId, string followedId);

        /// <returns>False when the user tries to follow themselves or the target user does not exist.</returns>
        Task<bool> FollowAsync(string followerId, string followedId);

        Task UnfollowAsync(string followerId, string followedId);

        Task<int> CountFollowersAsync(string userId);
        Task<int> CountFollowingAsync(string userId);

        /// <summary>
        /// Users who follow <paramref name="userId"/>, flagged with whether <paramref name="currentUserId"/> follows them.
        /// </summary>
        Task<List<FollowViewModel>> GetFollowersAsync(string userId, string? currentUserId);

        /// <summary>
        /// Users whom <paramref name="userId"/> follows, flagged with whether <paramref name="currentUserId"/> follows them.
        /// </summary>
        Task<List<FollowViewModel>> GetFollowingAsync(string userId, string? currentUserId);
    }
}
