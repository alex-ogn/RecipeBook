using Microsoft.EntityFrameworkCore;
using RecipeBook.Data;
using RecipeBook.Models;

namespace RecipeBook.Services
{
    public class RecipeCategoryService : IRecipeCategoryService
    {
        private readonly ApplicationDbContext _context;

        public RecipeCategoryService(ApplicationDbContext context)
        {
            _context = context;
        }

        public Task<List<RecipeCategory>> GetAllAsync()
            => _context.RecipeCategories.AsNoTracking().OrderBy(c => c.Name).ToListAsync();

        public Task<RecipeCategory?> GetByIdAsync(int id)
            => _context.RecipeCategories.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id);

        public Task<RecipeCategory?> FindByNamePartAsync(string namePart)
        {
            var part = namePart.Trim().ToLower();
            return _context.RecipeCategories.AsNoTracking().FirstOrDefaultAsync(c => c.Name.ToLower().Contains(part));
        }

        public Task<bool> NameExistsAsync(string name, int? excludeId = null)
            => _context.RecipeCategories.AnyAsync(c => c.Name == name && (excludeId == null || c.Id != excludeId));

        public async Task CreateAsync(RecipeCategory category)
        {
            _context.RecipeCategories.Add(category);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> UpdateAsync(RecipeCategory category)
        {
            try
            {
                _context.RecipeCategories.Update(category);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _context.RecipeCategories.AnyAsync(c => c.Id == category.Id))
                {
                    return false;
                }

                throw;
            }
        }

        public async Task<DeleteResult> DeleteAsync(int id)
        {
            var category = await _context.RecipeCategories.FindAsync(id);
            if (category == null)
            {
                return DeleteResult.NotFound();
            }

            var usedBy = await _context.Recipes
                .Where(r => r.RecipeCategoryId == id)
                .Select(r => r.Title)
                .ToListAsync();

            if (usedBy.Count > 0)
            {
                return DeleteResult.InUse(usedBy);
            }

            _context.RecipeCategories.Remove(category);
            await _context.SaveChangesAsync();
            return DeleteResult.Deleted();
        }
    }
}
