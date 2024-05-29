using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using RecipeBook.Data;
using RecipeBook.Models;
using RecipeBook.Views.Recipes.ViewModels;

namespace RecipeBook.Controllers
{
    [Authorize(Roles = "Admin,User")]
    public class RecipesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public RecipesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Recipes

        public async Task<IActionResult> Index()
        {
            bool isUserAdmin = User.IsInRole("Admin");
            string currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier); // Make sure you have using System.Security.Claims;

            ViewData["IsUserAdmin"] = isUserAdmin;
            ViewData["CurrentUserId"] = currentUserId;

            var applicationDbContext = _context.Recipies.Include(r => r.User);
            return View(await applicationDbContext.ToListAsync());
        }

        public async Task<IActionResult> MyIndex()
        {
            string userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var applicationDbContext = _context.Recipies.Include(r => r.User).Where(r => r.UserId == userId);
            return View(await applicationDbContext.ToListAsync());
        }

        [AllowAnonymous]
        public IActionResult GetImage(int id)
        {
            var recipe = _context.Recipies.FirstOrDefault(r => r.Id == id);
            if (recipe != null && recipe.Image != null)
            {
                return File(recipe.Image, "image/jpg");  // Assumes images are stored as JPEGs; adjust MIME type accordingly
            }
            else
            {
                return NotFound();  // Return a 404 Not Found if there is no image or recipe
            }
        }

        [AllowAnonymous]
        public byte[] GetImageBuffer(int id)
        {
            var recipe = _context.Recipies.FirstOrDefault(r => r.Id == id);
            return recipe.Image;
        }

        // GET: Recipes/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var recipe = await _context.Recipies
                                       .Include(r => r.RecipeIngredients)
                                           .ThenInclude(ri => ri.Ingredient)
                                       .FirstOrDefaultAsync(r => r.Id == id);

            if (recipe == null)
            {
                return NotFound();
            }

            bool isUserAdmin = User.IsInRole("Admin");
            string currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier); // Make sure you have using System.Security.Claims;
            ViewData["IsUserAdmin"] = isUserAdmin;
            ViewData["CurrentUserId"] = currentUserId;

            return View(recipe);
        }

        // GET: Recipes/Create
        public IActionResult Create()
        {
            ViewData["UserId"] = new SelectList(_context.Users, "Id", "Id");
            var ingredients = _context.Ingredients
                .Include(i => i.Category)
                .ToList();

            // Group ingredients by category in memory
            var ingredientsByCategory = ingredients
                .GroupBy(i => i.Category.Name)
                .OrderBy(comparer => comparer.Key)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(i => new RecipeIngredientViewModel
                    {
                        IngredientId = i.Id,
                        Name = i.Name
                    }).OrderBy(n => n.Name).ToArray()
                );

            var viewModel = new RecipeViewModel
            {
                IngredientsByCategory = ingredientsByCategory
            };

            bool isUserAdmin = User.IsInRole("Admin");
            string currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier); // Make sure you have using System.Security.Claims;

            ViewData["IsUserAdmin"] = isUserAdmin;
            ViewData["CurrentUserId"] = currentUserId;

            return View(viewModel);
        }

        // POST: Recipes/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(RecipeViewModel model, IFormFile imageFile, string SelectedIngredientsJson)
        {
            try
            {
                if (model.imageFileNew == null)
                {
                    ModelState.Remove("imageFileNew");
                }
                if (model.imageFile == null)
                {
                    ModelState.Remove("imageFile");
                }

                if (ModelState.IsValid)
                {
                    var recipe = new Recipe();
                    string userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                    model.EditRecipe(recipe);
                    recipe.UserId = userId;

                    if (imageFile != null && imageFile.Length > 0)
                    {
                        using (var memoryStream = new MemoryStream())
                        {
                            await imageFile.CopyToAsync(memoryStream);
                            recipe.Image = memoryStream.ToArray();  // Saving the image as a byte array
                        }
                    }

                    // Deserialize the SelectedIngredientsJson to populate the SelectedIngredients list
                    if (!string.IsNullOrEmpty(SelectedIngredientsJson))
                    {
                        model.SelectedIngredients = JsonConvert.DeserializeObject<List<RecipeIngredientViewModel>>(SelectedIngredientsJson);
                    }

                    model.RecipeIngredients = model.SelectedIngredients.Select(ingredient => new RecipeIngredient
                    {
                        IngredientId = ingredient.IngredientId,
                        QuantityNeeded = ingredient.QuantityNeeded
                    }).ToList();

                    _context.Recipies.Add(recipe);
                    await _context.SaveChangesAsync();

                    return RedirectToAction(nameof(Index));
                }

                foreach (var state in ModelState)
                {
                    foreach (var error in state.Value.Errors)
                    {
                        Console.WriteLine($"Error in {state.Key}: {error.ErrorMessage}");
                    }
                }

                // Re-populate the ingredients if needed, handle failures, etc.
                var ingredients = _context.Ingredients
                    .Include(i => i.Category)
                    .ToList();

                var ingredientsByCategory = ingredients
                    .GroupBy(i => i.Category.Name)
                    .OrderBy(comparer => comparer.Key)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(i => new RecipeIngredientViewModel
                        {
                            IngredientId = i.Id,
                            Name = i.Name
                        }).OrderBy(n => n.Name).ToArray()
                    );

                model.IngredientsByCategory = ingredientsByCategory;

                ViewData["UserId"] = new SelectList(_context.Users, "Id", "Id", model.UserId);
                return View(model);
            }
            catch (Exception ex)
            {
                // Log the exception (this could be to a file, database, or logging service)
                Console.WriteLine(ex.Message);

                // Optionally, rethrow or return a custom error view
                throw;
            }
        }

        // GET: Recipes/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || _context.Recipies == null)
            {
                return NotFound();
            }

            var recipe = await _context.Recipies
                .Include(r => r.RecipeIngredients)
                .ThenInclude(ri => ri.Ingredient)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (recipe == null)
            {
                return NotFound();
            }

            var ingredients = _context.Ingredients
                .Include(i => i.Category)
                .ToList();

            // Group ingredients by category in memory
            var ingredientsByCategory = ingredients
                .GroupBy(i => i.Category.Name)
                .OrderBy(comparer => comparer.Key)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(i => new RecipeIngredientViewModel
                    {
                        IngredientId = i.Id,
                        Name = i.Name,
                        QuantityNeeded = recipe.RecipeIngredients
                            .Where(ri => ri.IngredientId == i.Id)
                            .Select(ri => ri.QuantityNeeded)
                            .FirstOrDefault()
                    }).OrderBy(n => n.Name).ToArray()
                );


            var viewModel = new RecipeViewModel(recipe)
            {
                IngredientsByCategory = ingredientsByCategory,
                SelectedIngredients = recipe.RecipeIngredients
                    .Select(ri => new RecipeIngredientViewModel
                    {
                        IngredientId = ri.IngredientId,
                        Name = ri.Ingredient.Name,
                        QuantityNeeded = ri.QuantityNeeded
                    }).ToList()
            };

            ViewData["UserId"] = new SelectList(_context.Users, "Id", "Id", recipe.UserId);
            return View(viewModel);
        }

        // POST: Recipes/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, RecipeViewModel model, IFormFile imageFileNew, string SelectedIngredientsJson)
        {
            if (id != model.Id)
            {
                return NotFound();
            }

            if (model.imageFileNew == null)
            {
                ModelState.Remove("ImageFileNew");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var recipe = await _context.Recipies.FindAsync(model.Id);
                    if (recipe == null)
                    {
                        return NotFound();
                    }

                    model.EditRecipe(recipe);

                    if (imageFileNew != null && imageFileNew.Length > 0)
                    {
                        using (var memoryStream = new MemoryStream())
                        {
                            await imageFileNew.CopyToAsync(memoryStream);
                            recipe.Image = memoryStream.ToArray();  // Saving the image as a byte array
                        }
                    }

                    // Deserialize the SelectedIngredientsJson to populate the SelectedIngredients list
                    if (!string.IsNullOrEmpty(SelectedIngredientsJson))
                    {
                        model.SelectedIngredients = JsonConvert.DeserializeObject<List<RecipeIngredientViewModel>>(SelectedIngredientsJson);
                    }

                    // Update the RecipeIngredients
                    var existingIngredients = await _context.RecipeIngredients
                        .Where(ri => ri.RecipeId == id)
                        .ToListAsync();

                    _context.RecipeIngredients.RemoveRange(existingIngredients);

                    var newIngredients = model.SelectedIngredients.Select(ingredient => new RecipeIngredient
                    {
                        RecipeId = id,
                        IngredientId = ingredient.IngredientId,
                        QuantityNeeded = ingredient.QuantityNeeded
                    }).ToList();

                    _context.RecipeIngredients.AddRange(newIngredients);

                    _context.Update(recipe);
                    await _context.SaveChangesAsync();

                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!RecipeExists(model.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            foreach (var state in ModelState)
            {
                foreach (var error in state.Value.Errors)
                {
                    Console.WriteLine($"Error in {state.Key}: {error.ErrorMessage}");
                }
            }

            var recipeUnchanged = await _context.Recipies
               .Include(r => r.RecipeIngredients)
               .ThenInclude(ri => ri.Ingredient)
               .FirstOrDefaultAsync(m => m.Id == id);

            var ingredients = _context.Ingredients
               .Include(i => i.Category)
               .ToList();

            // Group ingredients by category in memory
            var ingredientsByCategory = ingredients
                .GroupBy(i => i.Category.Name)
                .OrderBy(comparer => comparer.Key)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(i => new RecipeIngredientViewModel
                    {
                        IngredientId = i.Id,
                        Name = i.Name,
                        QuantityNeeded = recipeUnchanged.RecipeIngredients
                            .Where(ri => ri.IngredientId == i.Id)
                            .Select(ri => ri.QuantityNeeded)
                            .FirstOrDefault()
                    }).OrderBy(n => n.Name).ToArray()
                );

            var viewModel = new RecipeViewModel(recipeUnchanged)
            {
                IngredientsByCategory = ingredientsByCategory,
                SelectedIngredients = recipeUnchanged.RecipeIngredients
                    .Select(ri => new RecipeIngredientViewModel
                    {
                        IngredientId = ri.IngredientId,
                        Name = ri.Ingredient.Name,
                        QuantityNeeded = ri.QuantityNeeded
                    }).ToList()
            };

            model.IngredientsByCategory = ingredientsByCategory;

            ViewData["UserId"] = new SelectList(_context.Users, "Id", "Id", model.UserId);
            return View(viewModel);
        }

        // GET: Recipes/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || _context.Recipies == null)
            {
                return NotFound();
            }

            var recipe = await _context.Recipies
                .Include(r => r.User)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (recipe == null)
            {
                return NotFound();
            }

            return View(recipe);
        }

        // POST: Recipes/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (_context.Recipies == null)
            {
                return Problem("Entity set 'ApplicationDbContext.Recipies'  is null.");
            }
            var recipe = await _context.Recipies.FindAsync(id);
            if (recipe != null)
            {
                _context.Recipies.Remove(recipe);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool RecipeExists(int id)
        {
            return (_context.Recipies?.Any(e => e.Id == id)).GetValueOrDefault();
        }
    }
}