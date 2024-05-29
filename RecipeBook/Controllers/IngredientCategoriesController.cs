using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using RecipeBook.Data;
using RecipeBook.Models;

namespace RecipeBook.Controllers
{
    [Authorize(Roles = "Admin")]
    public class IngredientCategoriesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public IngredientCategoriesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: IngredientCategories
        public async Task<IActionResult> Index()
        {
              return _context.IngredientCategories != null ? 
                          View(await _context.IngredientCategories.ToListAsync()) :
                          Problem("Entity set 'ApplicationDbContext.IngredientCategories'  is null.");
        }

        // GET: IngredientCategories/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null || _context.IngredientCategories == null)
            {
                return NotFound();
            }

            var ingredientCategory = await _context.IngredientCategories
                .FirstOrDefaultAsync(m => m.Id == id);
            if (ingredientCategory == null)
            {
                return NotFound();
            }

            return View(ingredientCategory);
        }

        // GET: IngredientCategories/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: IngredientCategories/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Name")] IngredientCategory ingredientCategory)
        {
            if (ModelState.IsValid)
            {
                _context.Add(ingredientCategory);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(ingredientCategory);
        }

        // GET: IngredientCategories/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || _context.IngredientCategories == null)
            {
                return NotFound();
            }

            var ingredientCategory = await _context.IngredientCategories.FindAsync(id);
            if (ingredientCategory == null)
            {
                return NotFound();
            }
            return View(ingredientCategory);
        }

        // POST: IngredientCategories/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name")] IngredientCategory ingredientCategory)
        {
            if (id != ingredientCategory.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(ingredientCategory);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!IngredientCategoryExists(ingredientCategory.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(ingredientCategory);
        }

        // GET: IngredientCategories/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || _context.IngredientCategories == null)
            {
                return NotFound();
            }

            var ingredientCategory = await _context.IngredientCategories
                .FirstOrDefaultAsync(m => m.Id == id);
            if (ingredientCategory == null)
            {
                return NotFound();
            }

            return View(ingredientCategory);
        }

        // POST: IngredientCategories/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (_context.IngredientCategories == null)
            {
                return Problem("Entity set 'ApplicationDbContext.IngredientCategories'  is null.");
            }
            var ingredientCategory = await _context.IngredientCategories.FindAsync(id);
            if (ingredientCategory != null)
            {
                _context.IngredientCategories.Remove(ingredientCategory);
            }
            
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool IngredientCategoryExists(int id)
        {
          return (_context.IngredientCategories?.Any(e => e.Id == id)).GetValueOrDefault();
        }
    }
}
