using Microsoft.EntityFrameworkCore;
using RecipeBook.Data;
using RecipeBook.Extensions;
using RecipeBook.Models;
using RecipeBook.ViewModels.Shared;

namespace RecipeBook.Services
{
    public class IngredientService : IIngredientService
    {
        private readonly ApplicationDbContext _context;

        public IngredientService(ApplicationDbContext context)
        {
            _context = context;
        }

        public Task<PagedResult<Ingredient>> SearchAsync(string? search, int? categoryId, int page, int pageSize)
        {
            var query = _context.Ingredients
                .AsNoTracking()
                .Include(i => i.IngredientCategory)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(i => i.Name.Contains(search));
            }

            if (categoryId.HasValue)
            {
                query = query.Where(i => i.IngredientCategoryId == categoryId);
            }

            return query.OrderBy(i => i.Name).ThenBy(i => i.Id).ToPagedResultAsync(page, pageSize);
        }

        public Task<Ingredient?> GetByIdAsync(int id)
            => _context.Ingredients
                .AsNoTracking()
                .Include(i => i.IngredientCategory)
                .FirstOrDefaultAsync(i => i.Id == id);

        public Task<List<IngredientOption>> GetOptionsAsync()
            => _context.Ingredients
                .AsNoTracking()
                .OrderBy(i => i.Name)
                .Select(i => new IngredientOption(i.Id, i.Name))
                .ToListAsync();

        public Task<bool> ExistsInCategoryAsync(string name, int categoryId, int? excludeId = null)
            => _context.Ingredients.AnyAsync(i =>
                i.Name == name
                && i.IngredientCategoryId == categoryId
                && (excludeId == null || i.Id != excludeId));

        public async Task CreateAsync(Ingredient ingredient)
        {
            _context.Ingredients.Add(ingredient);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> UpdateAsync(Ingredient ingredient)
        {
            try
            {
                _context.Ingredients.Update(ingredient);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _context.Ingredients.AnyAsync(i => i.Id == ingredient.Id))
                {
                    return false;
                }

                throw;
            }
        }

        public async Task<DeleteResult> DeleteAsync(int id)
        {
            var ingredient = await _context.Ingredients.FindAsync(id);
            if (ingredient == null)
            {
                return DeleteResult.NotFound();
            }

            var usedBy = await _context.RecipeIngredients
                .Where(ri => ri.IngredientId == id)
                .Select(ri => ri.Recipe.Title)
                .ToListAsync();

            if (usedBy.Count > 0)
            {
                return DeleteResult.InUse(usedBy);
            }

            _context.Ingredients.Remove(ingredient);
            await _context.SaveChangesAsync();
            return DeleteResult.Deleted();
        }
    }
}
