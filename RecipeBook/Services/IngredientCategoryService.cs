using Microsoft.EntityFrameworkCore;
using RecipeBook.Data;
using RecipeBook.Models;

namespace RecipeBook.Services
{
    public class IngredientCategoryService : IIngredientCategoryService
    {
        private readonly ApplicationDbContext _context;

        public IngredientCategoryService(ApplicationDbContext context)
        {
            _context = context;
        }

        public Task<List<IngredientCategory>> GetAllAsync()
            => _context.IngredientCategories.AsNoTracking().OrderBy(c => c.Name).ToListAsync();

        public Task<IngredientCategory?> GetByIdAsync(int id)
            => _context.IngredientCategories.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id);

        public Task<bool> NameExistsAsync(string name, int? excludeId = null)
            => _context.IngredientCategories.AnyAsync(c => c.Name == name && (excludeId == null || c.Id != excludeId));

        public async Task CreateAsync(IngredientCategory category)
        {
            _context.IngredientCategories.Add(category);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> UpdateAsync(IngredientCategory category)
        {
            try
            {
                _context.IngredientCategories.Update(category);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _context.IngredientCategories.AnyAsync(c => c.Id == category.Id))
                {
                    return false;
                }

                throw;
            }
        }

        public async Task<DeleteResult> DeleteAsync(int id)
        {
            var category = await _context.IngredientCategories.FindAsync(id);
            if (category == null)
            {
                return DeleteResult.NotFound();
            }

            var usedBy = await _context.Ingredients
                .Where(i => i.IngredientCategoryId == id)
                .Select(i => i.Name)
                .ToListAsync();

            if (usedBy.Count > 0)
            {
                return DeleteResult.InUse(usedBy);
            }

            _context.IngredientCategories.Remove(category);
            await _context.SaveChangesAsync();
            return DeleteResult.Deleted();
        }
    }
}
