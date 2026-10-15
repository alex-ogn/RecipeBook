using Microsoft.EntityFrameworkCore;
using RecipeBook.Data;
using RecipeBook.Models;
using RecipeBook.ViewModels.Users;

namespace RecipeBook.Services
{
    public class FollowService : IFollowService
    {
        private readonly ApplicationDbContext _context;

        public FollowService(ApplicationDbContext context)
        {
            _context = context;
        }

        public Task<bool> IsFollowingAsync(string? followerId, string followedId)
            => _context.UserFollowers.AnyAsync(f => f.FollowerId == followerId && f.FollowedId == followedId);

        public async Task<bool> FollowAsync(string followerId, string followedId)
        {
            if (followerId == followedId || !await _context.Users.AnyAsync(u => u.Id == followedId))
            {
                return false;
            }

            if (!await IsFollowingAsync(followerId, followedId))
            {
                _context.UserFollowers.Add(new UserFollower { FollowerId = followerId, FollowedId = followedId });
                await _context.SaveChangesAsync();
            }

            return true;
        }

        public async Task UnfollowAsync(string followerId, string followedId)
        {
            var follow = await _context.UserFollowers
                .FirstOrDefaultAsync(f => f.FollowerId == followerId && f.FollowedId == followedId);

            if (follow != null)
            {
                _context.UserFollowers.Remove(follow);
                await _context.SaveChangesAsync();
            }
        }

        public Task<int> CountFollowersAsync(string userId)
            => _context.UserFollowers.CountAsync(f => f.FollowedId == userId);

        public Task<int> CountFollowingAsync(string userId)
            => _context.UserFollowers.CountAsync(f => f.FollowerId == userId);

        public async Task<List<FollowViewModel>> GetFollowersAsync(string userId, string? currentUserId)
        {
            var users = await _context.UserFollowers
                .AsNoTracking()
                .Where(f => f.FollowedId == userId)
                .Select(f => f.Follower)
                .ToListAsync();

            return await ToFollowViewModelsAsync(users, currentUserId);
        }

        public async Task<List<FollowViewModel>> GetFollowingAsync(string userId, string? currentUserId)
        {
            var users = await _context.UserFollowers
                .AsNoTracking()
                .Where(f => f.FollowerId == userId)
                .Select(f => f.Followed)
                .ToListAsync();

            return await ToFollowViewModelsAsync(users, currentUserId);
        }

        /// <summary>
        /// Loads the current user's follows once instead of querying per listed user.
        /// </summary>
        private async Task<List<FollowViewModel>> ToFollowViewModelsAsync(List<ApplicationUser> users, string? currentUserId)
        {
            var listedIds = users.Select(u => u.Id).ToList();
            var followedByCurrentUser = currentUserId == null
                ? new HashSet<string>()
                : (await _context.UserFollowers
                    .Where(f => f.FollowerId == currentUserId && listedIds.Contains(f.FollowedId))
                    .Select(f => f.FollowedId)
                    .ToListAsync())
                    .ToHashSet();

            return users
                .Select(u => new FollowViewModel { User = u, IsFollowing = followedByCurrentUser.Contains(u.Id) })
                .ToList();
        }
    }
}
