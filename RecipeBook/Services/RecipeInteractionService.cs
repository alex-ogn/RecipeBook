using Microsoft.EntityFrameworkCore;
using RecipeBook.Data;
using RecipeBook.Models;

namespace RecipeBook.Services
{
    public class RecipeInteractionService : IRecipeInteractionService
    {
        private readonly ApplicationDbContext _context;

        public RecipeInteractionService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<bool> LikeAsync(int recipeId, string userId)
        {
            if (!await RecipeExistsAsync(recipeId))
            {
                return false;
            }

            if (!await _context.RecipeLikes.AnyAsync(l => l.RecipeId == recipeId && l.UserId == userId))
            {
                _context.RecipeLikes.Add(new RecipeLike { RecipeId = recipeId, UserId = userId });
                await _context.SaveChangesAsync();
            }

            return true;
        }

        public async Task UnlikeAsync(int recipeId, string userId)
        {
            var like = await _context.RecipeLikes.FirstOrDefaultAsync(l => l.RecipeId == recipeId && l.UserId == userId);
            if (like != null)
            {
                _context.RecipeLikes.Remove(like);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<bool> SaveAsync(int recipeId, string userId)
        {
            if (!await RecipeExistsAsync(recipeId))
            {
                return false;
            }

            if (!await _context.SavedRecipes.AnyAsync(s => s.RecipeId == recipeId && s.UserId == userId))
            {
                _context.SavedRecipes.Add(new SavedRecipe { RecipeId = recipeId, UserId = userId, DateSaved = DateTime.UtcNow });
                await _context.SaveChangesAsync();
            }

            return true;
        }

        public async Task UnsaveAsync(int recipeId, string userId)
        {
            var saved = await _context.SavedRecipes.FirstOrDefaultAsync(s => s.RecipeId == recipeId && s.UserId == userId);
            if (saved != null)
            {
                _context.SavedRecipes.Remove(saved);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<bool> AddCommentAsync(int recipeId, string userId, string content)
        {
            if (!await RecipeExistsAsync(recipeId))
            {
                return false;
            }

            _context.RecipeComments.Add(new RecipeComment
            {
                RecipeId = recipeId,
                UserId = userId,
                Content = content,
                CreatedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();
            return true;
        }

        public Task<RecipeComment?> GetCommentAsync(int commentId)
            => _context.RecipeComments
                .Include(c => c.Recipe)
                .Include(c => c.User)
                .FirstOrDefaultAsync(c => c.Id == commentId);

        public async Task UpdateCommentAsync(RecipeComment comment, string content)
        {
            comment.Content = content;
            await _context.SaveChangesAsync();
        }

        public async Task DeleteCommentAsync(RecipeComment comment)
        {
            _context.RecipeComments.Remove(comment);
            await _context.SaveChangesAsync();
        }

        private Task<bool> RecipeExistsAsync(int recipeId) => _context.Recipies.AnyAsync(r => r.Id == recipeId);
    }
}
