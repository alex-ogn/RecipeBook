using RecipeBook.Models;
using RecipeBook.Services;

namespace Tests.Services
{
    public class FollowAndInteractionServiceTests
    {
        [Fact]
        public async Task Follow_RejectsSelfAndUnknownUsers()
        {
            using var context = TestDb.Create();
            context.Users.Add(TestDb.User("a"));
            await context.SaveChangesAsync();
            var service = new FollowService(context);

            Assert.False(await service.FollowAsync("a", "a"));
            Assert.False(await service.FollowAsync("a", "missing"));
            Assert.Empty(context.UserFollowers);
        }

        [Fact]
        public async Task Follow_IsIdempotent_AndUnfollowRemoves()
        {
            using var context = TestDb.Create();
            context.Users.AddRange(TestDb.User("a"), TestDb.User("b"));
            await context.SaveChangesAsync();
            var service = new FollowService(context);

            Assert.True(await service.FollowAsync("a", "b"));
            Assert.True(await service.FollowAsync("a", "b"));
            Assert.Equal(1, await service.CountFollowersAsync("b"));
            Assert.Equal(1, await service.CountFollowingAsync("a"));

            await service.UnfollowAsync("a", "b");
            Assert.False(await service.IsFollowingAsync("a", "b"));
        }

        [Fact]
        public async Task GetFollowers_FlagsWhomTheCurrentUserFollows()
        {
            using var context = TestDb.Create();
            context.Users.AddRange(TestDb.User("me"), TestDb.User("star"), TestDb.User("fan1"), TestDb.User("fan2"));
            context.UserFollowers.AddRange(
                new UserFollower { FollowerId = "fan1", FollowedId = "star" },
                new UserFollower { FollowerId = "fan2", FollowedId = "star" },
                new UserFollower { FollowerId = "me", FollowedId = "fan1" });
            await context.SaveChangesAsync();
            var service = new FollowService(context);

            var followers = await service.GetFollowersAsync("star", currentUserId: "me");

            Assert.Equal(2, followers.Count);
            Assert.True(followers.Single(f => f.User.Id == "fan1").IsFollowing);
            Assert.False(followers.Single(f => f.User.Id == "fan2").IsFollowing);
        }

        [Fact]
        public async Task Like_ReturnsFalseForMissingRecipe_AndIsIdempotent()
        {
            using var context = TestDb.Create();
            context.Recipes.Add(TestDb.Recipe(1, "author"));
            await context.SaveChangesAsync();
            var service = new RecipeInteractionService(context);

            Assert.False(await service.LikeAsync(99, "u1"));
            Assert.True(await service.LikeAsync(1, "u1"));
            Assert.True(await service.LikeAsync(1, "u1"));
            Assert.Single(context.RecipeLikes);

            await service.UnlikeAsync(1, "u1");
            Assert.Empty(context.RecipeLikes);
        }

        [Fact]
        public async Task Save_IsIdempotent_AndUnsaveRemoves()
        {
            using var context = TestDb.Create();
            context.Recipes.Add(TestDb.Recipe(1, "author"));
            await context.SaveChangesAsync();
            var service = new RecipeInteractionService(context);

            Assert.True(await service.SaveAsync(1, "u1"));
            Assert.True(await service.SaveAsync(1, "u1"));
            Assert.Single(context.SavedRecipes);

            await service.UnsaveAsync(1, "u1");
            Assert.Empty(context.SavedRecipes);
        }

        [Fact]
        public async Task Comments_CanBeAddedUpdatedAndDeleted()
        {
            using var context = TestDb.Create();
            context.Users.Add(TestDb.User("u1"));
            context.Recipes.Add(TestDb.Recipe(1, "author"));
            await context.SaveChangesAsync();
            var service = new RecipeInteractionService(context);

            Assert.False(await service.AddCommentAsync(99, "u1", "Hi"));
            Assert.True(await service.AddCommentAsync(1, "u1", "Tasty"));

            var comment = Assert.Single(context.RecipeComments);
            var loaded = await service.GetCommentAsync(comment.Id);
            Assert.Equal("author", loaded!.Recipe.UserId);

            await service.UpdateCommentAsync(loaded, "Very tasty");
            Assert.Equal("Very tasty", context.RecipeComments.Single().Content);

            await service.DeleteCommentAsync(loaded);
            Assert.Empty(context.RecipeComments);
        }
    }
}
