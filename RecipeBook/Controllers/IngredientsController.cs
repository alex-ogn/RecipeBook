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
    public class IngredientsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public IngredientsController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string searchString, int? categoryId)
        {
            var categories = await _context.IngredientCategories.ToListAsync();
            ViewData["Categories"] = new SelectList(categories, "Id", "Name");
            ViewData["Search"] = searchString;
            ViewData["SelectedCategoryId"] = categoryId;

            var ingredientsQuery = _context.Ingredients
                .Include(i => i.IngredientCategory)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                ingredientsQuery = ingredientsQuery
                    .Where(i => i.Name.Contains(searchString));
            }

            if (categoryId.HasValue)
            {
                ingredientsQuery = ingredientsQuery
                    .Where(i => i.IngredientCategoryId == categoryId);
            }

            var ingredients = await ingredientsQuery.ToListAsync();
            return View(ingredients);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null || _context.Ingredients == null)
            {
                return NotFound();
            }

            var ingredient = await _context.Ingredients
                .Include(i => i.IngredientCategory)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (ingredient == null)
            {
                return NotFound();
            }

            return View(ingredient);
        }

        public IActionResult Create()
        {
            ViewData["IngredientCategoryId"] = new SelectList(_context.IngredientCategories, "Id", "Name");
            return View();
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Name,IngredientCategoryId")] Ingredient ingredient)
        {
            var exists = await _context.Ingredients
                     .AnyAsync(i => i.Name == ingredient.Name && i.IngredientCategoryId == ingredient.IngredientCategoryId );

            if (exists)
            {
                ModelState.AddModelError("", "Съставка с това име вече съществува в тази категория.");
                ViewData["IngredientCategoryId"] = new SelectList(_context.IngredientCategories, "Id", "Name", ingredient.IngredientCategoryId);
                return View(ingredient);
            }

            if (ModelState.IsValid)
            {
                _context.Add(ingredient);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["IngredientCategoryId"] = new SelectList(_context.IngredientCategories, "Id", "Name", ingredient.IngredientCategoryId);
            return View(ingredient);
        }


        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || _context.Ingredients == null)
            {
                return NotFound();
            }

            var ingredient = await _context.Ingredients.FindAsync(id);
            if (ingredient == null)
            {
                return NotFound();
            }
            ViewData["IngredientCategoryId"] = new SelectList(_context.IngredientCategories, "Id", "Name", ingredient.IngredientCategoryId);
            return View(ingredient);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,IngredientCategoryId")] Ingredient ingredient)
        {
            if (id != ingredient.Id)
            {
                return NotFound();
            }

            var exists = await _context.Ingredients
                .AnyAsync(i => i.Name == ingredient.Name && i.IngredientCategoryId == ingredient.IngredientCategoryId && i.Id != ingredient.Id);

            if (exists)
            {
                ModelState.AddModelError("", "Съставка с това име вече съществува в тази категория.");
                ViewData["IngredientCategoryId"] = new SelectList(_context.IngredientCategories, "Id", "Name", ingredient.IngredientCategoryId);
                return View(ingredient);
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(ingredient);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!IngredientExists(ingredient.Id))
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
            ViewData["IngredientCategoryId"] = new SelectList(_context.IngredientCategories, "Id", "Name", ingredient.IngredientCategoryId);
            return View(ingredient);
        }


        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || _context.Ingredients == null)
            {
                return NotFound();
            }

            var ingredient = await _context.Ingredients
                .Include(i => i.IngredientCategory)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (ingredient == null)
            {
                return NotFound();
            }

            return View(ingredient);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var ingredient = await _context.Ingredients
                .Include(i => i.RecipeIngredients)
                .ThenInclude(ri => ri.Recipe)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (ingredient == null)
                return NotFound();

            if (ingredient.RecipeIngredients.Any())
            {
                var usedInRecipes = ingredient.RecipeIngredients
                    .Select(ri => ri.Recipe.Title)
                    .ToList();

                var recipeNames = string.Join(", ", usedInRecipes);

                TempData["ErrorMessage"] = $"Съставката се използва в рецепти: {recipeNames}. Моля, първо я премахни от тях.";
                return RedirectToAction(nameof(Index));
            }

            _context.Ingredients.Remove(ingredient);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Съставката беше изтрита успешно.";
            return RedirectToAction(nameof(Index));
        }


        private bool IngredientExists(int id)
        {
          return (_context.Ingredients?.Any(e => e.Id == id)).GetValueOrDefault();
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult GetAll()
        {
            var ingredients = _context.Ingredients
                .Select(i => new { id = i.Id, name = i.Name })
                .ToList();
            return Json(ingredients);
        }

        [HttpPost]
        public IActionResult CreateFromRecipe(string name)
        {
            var existing = _context.Ingredients.FirstOrDefault(i => i.Name == name);
            if (existing != null)
                return Json(new { id = existing.Id, name = existing.Name });

            var ingredient = new Ingredient { Name = name };
            _context.Ingredients.Add(ingredient);
            _context.SaveChanges();

            return Json(new { id = ingredient.Id, name = ingredient.Name });
        }

    }
}
