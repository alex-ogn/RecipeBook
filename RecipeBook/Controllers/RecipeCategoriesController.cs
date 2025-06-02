using System;
using System.Collections.Generic;
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
    public class RecipeCategoriesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public RecipeCategoriesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: RecipeCategories
        public async Task<IActionResult> Index()
        {
              return _context.RecipeCategories != null ? 
                          View(await _context.RecipeCategories.ToListAsync()) :
                          Problem("Entity set 'ApplicationDbContext.RecipeCategories'  is null.");
        }

        // GET: RecipeCategories/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null || _context.RecipeCategories == null)
            {
                return NotFound();
            }

            var recipeCategory = await _context.RecipeCategories
                .FirstOrDefaultAsync(m => m.Id == id);
            if (recipeCategory == null)
            {
                return NotFound();
            }

            return View(recipeCategory);
        }

        // GET: RecipeCategories/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: RecipeCategories/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Name")] RecipeCategory recipeCategory)
        {
            if (ModelState.IsValid)
            {
                _context.Add(recipeCategory);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(recipeCategory);
        }

        // GET: RecipeCategories/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || _context.RecipeCategories == null)
            {
                return NotFound();
            }

            var recipeCategory = await _context.RecipeCategories.FindAsync(id);
            if (recipeCategory == null)
            {
                return NotFound();
            }
            return View(recipeCategory);
        }

        // POST: RecipeCategories/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name")] RecipeCategory recipeCategory)
        {
            if (id != recipeCategory.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(recipeCategory);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!RecipeCategoryExists(recipeCategory.Id))
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
            return View(recipeCategory);
        }

        // GET: RecipeCategories/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || _context.RecipeCategories == null)
            {
                return NotFound();
            }

            var recipeCategory = await _context.RecipeCategories
                .FirstOrDefaultAsync(m => m.Id == id);
            if (recipeCategory == null)
            {
                return NotFound();
            }

            return View(recipeCategory);
        }

        // POST: RecipeCategories/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (_context.RecipeCategories == null)
            {
                return Problem("Entity set 'ApplicationDbContext.RecipeCategories'  is null.");
            }
            var recipeCategory = await _context.RecipeCategories.FindAsync(id);
            if (recipeCategory != null)
            {
                _context.RecipeCategories.Remove(recipeCategory);
            }
            
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool RecipeCategoryExists(int id)
        {
          return (_context.RecipeCategories?.Any(e => e.Id == id)).GetValueOrDefault();
        }
    }
}
