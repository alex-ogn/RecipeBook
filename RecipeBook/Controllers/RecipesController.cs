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
using RecipeBook.Services;
using RecipeBook.Views.Recipes.ViewModels;
using Ganss.Xss;
using System.Text.RegularExpressions;
using RecipeBook.Views.Enums;

namespace RecipeBook.Controllers
{
    [Authorize(Roles = "Admin,User")]
    public class RecipesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IRecipePdfService _pdfService;

        public RecipesController(ApplicationDbContext context, IRecipePdfService pdfService)
        {
            _context = context;
            _pdfService = pdfService;
        }

        public async Task<IActionResult> Index(int? categoryId, RecipeSortOption sortOrder = RecipeSortOption.Newest)
        {
            string currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            ViewData["CurrentUserId"] = currentUserId;
            ViewData["CurrentAction"] = "Index";

            var query = _context.Recipies
                .Include(r => r.User)
                .Include(r => r.Category)
                .Include(r => r.Likes)
                .AsQueryable();

            if (categoryId.HasValue)
            {
                query = query.Where(r => r.Category.Id == categoryId);
            }

            query = sortOrder switch
            {
                RecipeSortOption.MostLiked => query.OrderByDescending(r => r.Likes.Count),
                RecipeSortOption.MostViewed => query.OrderByDescending(r => r.ViewCount),
                _ => query.OrderByDescending(r => r.Id)
            };

            var recipes = await query.ToListAsync();
            var recipeCards = MapToCardViewModels(recipes, currentUserId);

            var viewModel = new RecipeFilterViewModel
            {
                Recipes = recipeCards,
                Categories = new SelectList(_context.RecipeCategories, "Id", "Name"),
                SelectedCategoryId = categoryId,
                SortOrder = sortOrder
            };

            return View(viewModel);
        }

        public async Task<IActionResult> MyIndex(string? id)
        {
            // Ако id не е подадено, използваме текущия потребител
            string userId = id ?? User.FindFirstValue(ClaimTypes.NameIdentifier);

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null) return NotFound();

            var recipes = await _context.Recipies
                .Include(r => r.User)
                 .Include(r => r.Likes)
                .Where(r => r.UserId == userId)
                .ToListAsync();

            var isFollowing = await _context.UserFollowers
               .AnyAsync(f => f.FollowerId == currentUserId && f.FollowedId == id);

            var followersCount = _context.UserFollowers.Count(f => f.FollowedId == user.Id);
            var followingCount = _context.UserFollowers.Count(f => f.FollowerId == user.Id);

            ViewData["User"] = user;
            ViewData["CurrentUserId"] = currentUserId;
            ViewData["IsFollowing"] = isFollowing;
            ViewData["FollowersCount"] = followersCount;
            ViewData["FollowingCount"] = followingCount;

            var cardViewModels = MapToCardViewModels(recipes, currentUserId);
            return View(cardViewModels);
        }

        [AllowAnonymous]
        public IActionResult GetImage(int id)
        {
            var recipe = _context.Recipies.FirstOrDefault(r => r.Id == id);
            if (recipe != null && recipe.Image != null)
            {
                return File(recipe.Image, "image/jpg");  
            }
            else
            {
                return NotFound();  
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
                                       .Include(r => r.Category)
                                       .Include(r => r.User)
                                       .Include(r => r.SavedByUsers)
                                       .FirstOrDefaultAsync(r => r.Id == id);

            if (recipe == null)
            {
                return NotFound();
            }

            string currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var cookieKey = $"viewed_recipe_{id}";

            // Ако потребителят не е автор и няма cookie:
            if (recipe.UserId != currentUserId && !Request.Cookies.ContainsKey(cookieKey))
            {
                recipe.ViewCount++;
                _context.Update(recipe);
                await _context.SaveChangesAsync();

                // cookie за 1 час
                Response.Cookies.Append(cookieKey, "1", new CookieOptions
                {
                    Expires = DateTimeOffset.UtcNow.AddHours(1)
                });
            }

            bool isSaved = recipe.SavedByUsers
                                 .Any(s => s.UserId == currentUserId);

            RecipeViewModel recipeViewModel = new RecipeViewModel(recipe);

            bool isLiked = await _context.RecipeLikes
                .AnyAsync(l => l.RecipeId == id && l.UserId == currentUserId);

            ViewData["IsLiked"] = isLiked;
            ViewData["LikeCount"] = await _context.RecipeLikes.CountAsync(l => l.RecipeId == id);

            bool isUserAdmin = User.IsInRole("Admin");
            ViewData["IsUserAdmin"] = isUserAdmin;
            ViewData["CurrentUserId"] = currentUserId;
            ViewData["IsSaved"] = isSaved;

            return View(recipeViewModel);
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

            var viewModel = new RecipeEditViewModel
            {
                IngredientsByCategory = ingredientsByCategory
            };

            bool isUserAdmin = User.IsInRole("Admin");
            string currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier); // Make sure you have using System.Security.Claims;

            ViewData["IsUserAdmin"] = isUserAdmin;
            ViewData["CurrentUserId"] = currentUserId;
            viewModel.Categories = new SelectList(_context.RecipeCategories, "Id", "Name");

            return View(viewModel);
        }

        // POST: Recipes/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(RecipeEditViewModel model, IFormFile imageFile, string SelectedIngredientsJson)
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
                if (model.CategoryId == null)
                {
                    ModelState.Remove("CategoryId");
                }
                if (model.Categories == null)
                {
                    ModelState.Remove("Categories");
                }

                if (ModelState.IsValid)
                {

                    var recipe = new Recipe();
                    string userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                    model.EditRecipe(recipe);
                    recipe.UserId = userId;
                    recipe.Category = _context.RecipeCategories.Find(model.CategoryId.Value);

                    var sanitizer = new HtmlSanitizer();
                    sanitizer.AllowedTags.Add("b");
                    sanitizer.AllowedTags.Add("i");
                    sanitizer.AllowedTags.Add("ul");
                    sanitizer.AllowedTags.Add("ol");
                    sanitizer.AllowedTags.Add("li");
                    sanitizer.AllowedTags.Add("p");
                    sanitizer.AllowedTags.Add("span");
                    sanitizer.AllowedTags.Add("strong");
                    sanitizer.AllowedTags.Add("em");
                    sanitizer.AllowedTags.Add("br");

                    // Почистване на Description/Instructions
                    recipe.Description = sanitizer.Sanitize(recipe.Description);
                    recipe.Instructions = sanitizer.Sanitize(recipe.Instructions);

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

                    recipe.RecipeIngredients = model.RecipeIngredients;
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
                model.Categories = new SelectList(_context.RecipeCategories, "Id", "Name", model.CategoryId);
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
                .Include(r => r.Category)
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


            var viewModel = new RecipeEditViewModel(recipe)
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

            viewModel.Categories = new SelectList(_context.RecipeCategories, "Id", "Name", recipe.Category.Id);

            ViewData["UserId"] = new SelectList(_context.Users, "Id", "Id", recipe.UserId);
            return View(viewModel);
        }

        // POST: Recipes/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, RecipeEditViewModel model, IFormFile imageFileNew, string SelectedIngredientsJson)
        {
            if (id != model.Id)
            {
                return NotFound();
            }

            if (model.imageFileNew == null)
            {
                ModelState.Remove("ImageFileNew");
            }

            if (model.CategoryId == null)
            {
                ModelState.Remove("CategoryId");
            }
            if (model.Categories == null)
            {
                ModelState.Remove("Categories");
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
                    recipe.Category = _context.RecipeCategories.Find(model.CategoryId.Value);

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

            var viewModel = new RecipeEditViewModel(recipeUnchanged)
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
            model.Categories = new SelectList(_context.RecipeCategories, "Id", "Name", model.CategoryId);
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

            ViewData["Category"] = new SelectList(_context.RecipeCategories, "Id", "Name");
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
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> SaveRecipe(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var alreadySaved = await _context.SavedRecipes
                .AnyAsync(sr => sr.UserId == userId && sr.RecipeId == id);

            if (!alreadySaved)
            {
                var save = new SavedRecipe
                {
                    UserId = userId,
                    RecipeId = id,
                    DateSaved = DateTime.UtcNow
                };

                _context.SavedRecipes.Add(save);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Details", new { id });
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> UnsaveRecipe(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var saved = await _context.SavedRecipes
                .FirstOrDefaultAsync(sr => sr.UserId == userId && sr.RecipeId == id);

            if (saved != null)
            {
                _context.SavedRecipes.Remove(saved);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Details", new { id });
        }

        [Authorize]
        public async Task<IActionResult> LikedRecipes(int? categoryId)
{
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var likedRecipeIds = await _context.RecipeLikes
                .Where(sr => sr.UserId == userId)
                .Select(sr => sr.RecipeId)
                .ToListAsync();

            var recipesQuery = _context.Recipies
                .Include(r => r.Category)
                .Include(r => r.Likes)
                .Where(r => likedRecipeIds.Contains(r.Id));

            if (categoryId.HasValue)
            {
                recipesQuery = recipesQuery.Where(r => r.Category.Id == categoryId.Value);
            }

            var recipes = await recipesQuery.ToListAsync();
            var cardViewModels = MapToCardViewModels(recipes, userId);

            var categories = await _context.RecipeCategories
                .Select(c => new SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = c.Name
                }).ToListAsync();

            var model = new RecipeFilterViewModel
            {
                Recipes = cardViewModels,
                Categories = new SelectList(categories, "Value", "Text"),
                SelectedCategoryId = categoryId
            };

            ViewData["IsUserAdmin"] = User.IsInRole("Admin");
            ViewData["CurrentUserId"] = userId;
            ViewData["CurrentAction"] = "SavedRecipes";

            return View("Index", model);
        }
        [Authorize]
        public async Task<IActionResult> SavedRecipes(int? categoryId)
{
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var savedRecipeIds = await _context.SavedRecipes
                .Where(sr => sr.UserId == userId)
                .Select(sr => sr.RecipeId)
                .ToListAsync();

            var recipesQuery = _context.Recipies
                .Include(r => r.Category)
                .Where(r => savedRecipeIds.Contains(r.Id));

            if (categoryId.HasValue)
            {
                recipesQuery = recipesQuery.Where(r => r.Category.Id == categoryId.Value);
            }

            var recipes = await recipesQuery.ToListAsync();
            var cardViewModels = MapToCardViewModels(recipes, userId);

            var categories = await _context.RecipeCategories
                .Select(c => new SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = c.Name
                }).ToListAsync();

            var model = new RecipeFilterViewModel
            {
                Recipes = cardViewModels,
                Categories = new SelectList(categories, "Value", "Text"),
                SelectedCategoryId = categoryId
            };

            ViewData["IsUserAdmin"] = User.IsInRole("Admin");
            ViewData["CurrentUserId"] = userId;
            ViewData["CurrentAction"] = "SavedRecipes";

            return View("Index", model);
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Like(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var alreadyLiked = await _context.RecipeLikes
                .AnyAsync(l => l.RecipeId == id && l.UserId == userId);

            if (!alreadyLiked)
            {
                _context.RecipeLikes.Add(new RecipeLike
                {
                    RecipeId = id,
                    UserId = userId
                });

                await _context.SaveChangesAsync();
            }

            return Redirect(Request.Headers["Referer"].ToString());
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Unlike(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var like = await _context.RecipeLikes
                .FirstOrDefaultAsync(l => l.RecipeId == id && l.UserId == userId);

            if (like != null)
            {
                _context.RecipeLikes.Remove(like);
                await _context.SaveChangesAsync();
            }

            return Redirect(Request.Headers["Referer"].ToString());
        }

        public async Task<IActionResult> ExportPdf(int id)
        {
            var recipe = await _context.Recipies
                .Include(r => r.RecipeIngredients).ThenInclude(ri => ri.Ingredient)
                .Include(r => r.Category)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (recipe == null)
                return NotFound();

            var pdf = _pdfService.GenerateRecipePdf(recipe);
            return File(pdf, "application/pdf", $"{recipe.Title}.pdf");
        }

        private List<RecipeCardViewModel> MapToCardViewModels(IEnumerable<Recipe> recipes, string currentUserId)
        {
            bool isUserAdmin = User.IsInRole("Admin");

            return recipes.Select(r => new RecipeCardViewModel
            {
                Id = r.Id,
                Title = r.Title,
                DescriptionPreview = Regex.Replace(r.Description ?? "", "<.*?>", "")
                    .Substring(0, Math.Min(100, Regex.Replace(r.Description ?? "", "<.*?>", "").Length)) + "...",
                ImageUrl = Url.Action("GetImage", "Recipes", new { id = r.Id }),
                UserName = r.User?.UserName ?? "Потребител",
                UserId = r.UserId,
                LikesCount = r.Likes?.Count ?? 0,
                ViewCount = r.ViewCount,
                IsOwner = r.UserId == currentUserId || isUserAdmin
            }).ToList();
        }

    }
}
