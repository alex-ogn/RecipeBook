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
using Ganss.Xss;
using RecipeBook.ViewModels.Recipes;
using RecipeBook.ViewModels.Enums;
using System.ComponentModel.DataAnnotations;
using RecipeBook.Constants;

namespace RecipeBook.Controllers
{
    public class RecipesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IRecipePdfService _pdfService;
        private readonly IRecipeService _recipeService;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly string adddedByUserIngredientName = "Добавена от потребител";

        public RecipesController(ApplicationDbContext context, IRecipePdfService pdfService, IRecipeService recipeService, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _pdfService = pdfService;
            _recipeService = recipeService;
            _webHostEnvironment = webHostEnvironment;
        }

        public async Task<IActionResult> Index(int? SelectedCategoryId, RecipeSortOption sortOrder = RecipeSortOption.Newest)
        {
            string currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            ViewData["CurrentUserId"] = currentUserId;
            ViewData["CurrentAction"] = "Index";

            var query = _context.Recipies
                .Include(r => r.User)
                .Include(r => r.RecipeCategory)
                .Include(r => r.Likes)
                .AsQueryable();

            if (SelectedCategoryId.HasValue)
            {
                query = query.Where(r => r.RecipeCategory.Id == SelectedCategoryId);
            }

            query = sortOrder switch
            {
                RecipeSortOption.MostLiked => query.OrderByDescending(r => r.Likes.Count),
                RecipeSortOption.MostViewed => query.OrderByDescending(r => r.ViewCount),
                _ => query.OrderByDescending(r => r.Id)
            };

            var recipes = await query.ToListAsync();
            var recipeCards = RecipeCardMapper.ToCards(recipes, currentUserId, User.IsInRole(UserRoles.Admin));

            var viewModel = new RecipeFilterViewModel
            {
                Recipes = recipeCards,
                Categories = new SelectList(_context.RecipeCategories, "Id", "Name"),
                SelectedCategoryId = SelectedCategoryId,
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

            var cardViewModels = RecipeCardMapper.ToCards(recipes, currentUserId, User.IsInRole(UserRoles.Admin));
            return View(cardViewModels);
        }

        // GET: Recipes/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var recipe = await _context.Recipies
                                       .Include(r => r.RecipeIngredients)
                                           .ThenInclude(ri => ri.Ingredient)
                                                .ThenInclude(i => i.IngredientCategory)
                                       .Include(r => r.RecipeCategory)
                                       .Include(r => r.User)
                                       .Include(r => r.SavedByUsers)
                                       .Include(r => r.Comments)
                                       .ThenInclude(c => c.User)
                                       .FirstOrDefaultAsync(r => r.Id == id);

            if (recipe == null)
            {
                return NotFound();
            }

            string currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var similarRecipes = await _recipeService.GetSimilarRecipesAsync(recipe, currentUserId);

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

            RecipeViewModel recipeViewModel = new RecipeViewModel(recipe, similarRecipes);

            bool isLiked = await _context.RecipeLikes
                .AnyAsync(l => l.RecipeId == id && l.UserId == currentUserId);

            ViewData["IsLiked"] = isLiked;
            ViewData["LikeCount"] = await _context.RecipeLikes.CountAsync(l => l.RecipeId == id);

            bool isUserAdmin = User.IsInRole(UserRoles.Admin);
            ViewData["IsUserAdmin"] = isUserAdmin;
            ViewData["CurrentUserId"] = currentUserId;
            ViewData["IsSaved"] = isSaved;

            return View(recipeViewModel);
        }

        [Authorize(Roles = UserRoles.Admin + "," + UserRoles.User)]
        public IActionResult Create()
        {
            var viewModel = new RecipeEditViewModel
            {
                Categories = new SelectList(_context.RecipeCategories, "Id", "Name")
            };

            ViewData["UserId"] = new SelectList(_context.Users, "Id", "Id");
            ViewData["IsUserAdmin"] = User.IsInRole(UserRoles.Admin);
            ViewData["CurrentUserId"] = User.FindFirstValue(ClaimTypes.NameIdentifier);

            return View(viewModel);
        }


        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(RecipeEditViewModel model, string SelectedIngredientsJson)
        {
            try
            {
                ModelState.Remove("ImageUrl");
                ModelState.Remove("imageFileNew");

                if (model.CategoryId == null)
                    ModelState.Remove("CategoryId");
                if (model.Categories == null)
                    ModelState.Remove("Categories");

                if (model.imageFileNew != null && model.imageFileNew.Length > 0)
                {
                    // Ако има стара качена снимка при предишен неуспешен опит, я изтриваме
                    if (!string.IsNullOrEmpty(model.ImageUrl))
                    {
                        _recipeService.DeleteExistingImage(model.ImageUrl);
                    }

                    // Записваме новия файл на диска
                    model.ImageUrl = await SaveImageAsync(model.imageFileNew);
                }

                if (!ModelState.IsValid)
                {
                    model.Categories = new SelectList(_context.RecipeCategories, "Id", "Name", model.CategoryId);
                    ViewData["UserId"] = new SelectList(_context.Users, "Id", "Id", model.UserId);
                    ViewData["CurrentUserId"] = User.FindFirstValue(ClaimTypes.NameIdentifier);

                    return View(model);
                }

                var recipe = new Recipe();
                string userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                model.EditRecipe(recipe);
                SanitizeRecipeContent(recipe);

                recipe.UserId = userId;
                recipe.RecipeCategory = _context.RecipeCategories.Find(model.CategoryId.Value);

                // Обработка на съставките
                recipe.RecipeIngredients = await ProcessIngredientsAsync(SelectedIngredientsJson, recipe.Id);

                _context.Recipies.Add(recipe);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                model.Categories = new SelectList(_context.RecipeCategories, "Id", "Name", model.CategoryId);
                return View(model);
            }
        }


        [Authorize]
        // GET: Recipes/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || _context.Recipies == null)
            {
                return NotFound();
            }

            var recipe = await _context.Recipies
                .Include(r => r.RecipeCategory)
                .Include(r => r.RecipeIngredients)
                .ThenInclude(ri => ri.Ingredient)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (recipe == null)
            {
                return NotFound();
            }

            var ingredients = _context.Ingredients
                .Include(i => i.IngredientCategory)
                .ToList();

            // Group ingredients by category in memory
            var ingredientsByCategory = ingredients
                .GroupBy(i => i.IngredientCategory.Name)
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

            viewModel.Categories = new SelectList(_context.RecipeCategories, "Id", "Name", recipe.RecipeCategory.Id);

            ViewData["UserId"] = new SelectList(_context.Users, "Id", "Id", recipe.UserId);
            return View(viewModel);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, RecipeEditViewModel model, IFormFile imageFileNew, string SelectedIngredientsJson)
        {
            if (id != model.Id)
                return NotFound();

            if (model.ImageUrl == null)
                ModelState.Remove("ImageUrl");

            if (model.CategoryId == null)
                ModelState.Remove("CategoryId");

            if (model.Categories == null)
                ModelState.Remove("Categories");

            if (!ModelState.IsValid)
            {
                model.Categories = new SelectList(_context.RecipeCategories, "Id", "Name", model.CategoryId);
                ViewData["UserId"] = new SelectList(_context.Users, "Id", "Id", model.UserId);
                return View(model);
            }

            try
            {
                var recipe = await _context.Recipies.FindAsync(model.Id);
                if (recipe == null)
                    return NotFound();

                model.EditRecipe(recipe);
                SanitizeRecipeContent(recipe);

                recipe.RecipeCategory = _context.RecipeCategories.Find(model.CategoryId.Value);

                // Обработка на съставките
                var recipeIngredients = await ProcessIngredientsAsync(SelectedIngredientsJson, recipe.Id);

                // Изчистваме старите съставки
                var existing = await _context.RecipeIngredients
                    .Where(ri => ri.RecipeId == recipe.Id)
                    .ToListAsync();

                _context.RecipeIngredients.RemoveRange(existing);
                _context.RecipeIngredients.AddRange(recipeIngredients);

                _context.Update(recipe);
                await _context.SaveChangesAsync();

                if (model.imageFileNew != null && model.imageFileNew.Length > 0)
                {
                    //Изтриваме старата снимка от диска, ако съществува
                    _recipeService.DeleteExistingImage(recipe.ImageUrl);

                    // Запазваме новата снимка
                    recipe.ImageUrl = await SaveImageAsync(model.imageFileNew);
                }

                return RedirectToAction(nameof(Index));
            }
            catch (ValidationException vex)
            {
                ModelState.AddModelError(string.Empty, vex.Message);
                model.Categories = new SelectList(_context.RecipeCategories, "Id", "Name", model.CategoryId);
                return View(model);
            }
        }


        [Authorize]
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
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var success = await _recipeService.DeleteRecipeAsync(id, currentUserId);

            if (!success)
                return Forbid();

            TempData["SuccessMessage"] = "Рецептата е изтрита успешно.";
            return RedirectToAction(nameof(Index));
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
                .Include(r => r.RecipeCategory)
                .Include(r => r.Likes)
                .Where(r => likedRecipeIds.Contains(r.Id));

            if (categoryId.HasValue)
            {
                recipesQuery = recipesQuery.Where(r => r.RecipeCategory.Id == categoryId.Value);
            }

            var recipes = await recipesQuery.ToListAsync();
            var cardViewModels = RecipeCardMapper.ToCards(recipes, userId, User.IsInRole(UserRoles.Admin));

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

            ViewData["IsUserAdmin"] = User.IsInRole(UserRoles.Admin);
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
                .Include(r => r.RecipeCategory)
                .Include(r => r.Likes)
                .Where(r => savedRecipeIds.Contains(r.Id));

            if (categoryId.HasValue)
            {
                recipesQuery = recipesQuery.Where(r => r.RecipeCategory.Id == categoryId.Value);
            }

            var recipes = await recipesQuery.ToListAsync();
            var cardViewModels = RecipeCardMapper.ToCards(recipes, userId, User.IsInRole(UserRoles.Admin));

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

            ViewData["IsUserAdmin"] = User.IsInRole(UserRoles.Admin);
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

        [HttpGet]
        public async Task<IActionResult> ExportPdf(int id)
        {
            var recipe = await _context.Recipies
                .Include(r => r.RecipeIngredients).ThenInclude(ri => ri.Ingredient)
                .Include(r => r.RecipeCategory)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (recipe == null)
                return NotFound();

            // Подаваме webHostEnvironment на сервиза за PDF
            var pdf = _pdfService.GenerateRecipePdf(recipe, _webHostEnvironment);

            return File(pdf, "application/pdf", $"{recipe.Title}.pdf");
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddComment(int recipeId, string content)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(content)) return RedirectToAction("Details", "Recipes", new { id = recipeId });

            var comment = new RecipeComment
            {
                Content = content,
                RecipeId = recipeId,
                UserId = userId,
                CreatedAt = DateTime.UtcNow
            };

            _context.RecipeComments.Add(comment);
            await _context.SaveChangesAsync();

            return RedirectToAction("Details", "Recipes", new { id = recipeId });
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> DeleteComment(int id)
        {
            var comment = await _context.RecipeComments.Include(c => c.Recipe).FirstOrDefaultAsync(c => c.Id == id);
            if (comment == null) return NotFound();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            bool isAdmin = User.IsInRole(UserRoles.Admin);
            bool isOwner = comment.UserId == userId;
            bool isRecipeOwner = comment.Recipe.UserId == userId;

            if (!isAdmin && !isOwner && !isRecipeOwner)
                return Forbid();

            _context.RecipeComments.Remove(comment);
            await _context.SaveChangesAsync();

            return RedirectToAction("Details", "Recipes", new { id = comment.RecipeId });
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> EditComment(int id)
        {
            var comment = await _context.RecipeComments.Include(c => c.User).FirstOrDefaultAsync(c => c.Id == id);
            if (comment == null) return NotFound();

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (comment.UserId != currentUserId && !User.IsInRole(UserRoles.Admin))
                return Forbid();

            var model = new CommentEditViewModel
            {
                Id = comment.Id,
                Content = comment.Content
            };

            return View(model);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditComment(CommentEditViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var comment = await _context.RecipeComments.FindAsync(model.Id);
            if (comment == null) return NotFound();

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (comment.UserId != currentUserId && !User.IsInRole(UserRoles.Admin))
                return Forbid();

            comment.Content = model.Content;

            await _context.SaveChangesAsync();
            return RedirectToAction("Details", "Recipes", new { id = comment.RecipeId });
        }

        [Authorize]
        public async Task<IActionResult> FollowedUsersRecipes(int? categoryId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // ID на хората, които следвам
            var followedUserIds = await _context.UserFollowers
                .Where(f => f.FollowerId == userId)
                .Select(f => f.FollowedId)
                .ToListAsync();

            // Вземи рецептите на хората, които следваш
            var recipesQuery = _context.Recipies
                .Include(r => r.Likes)
                .Include(r => r.User)
                .Where(r => followedUserIds.Contains(r.UserId));

            if (categoryId.HasValue)
            {
                recipesQuery = recipesQuery.Where(r => r.RecipeCategory.Id == categoryId.Value);
            }

            var recipes = await recipesQuery.ToListAsync();
            var cardViewModels = RecipeCardMapper.ToCards(recipes, userId, User.IsInRole(UserRoles.Admin));

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

            ViewData["IsUserAdmin"] = User.IsInRole(UserRoles.Admin);
            ViewData["CurrentUserId"] = userId;
            ViewData["CurrentAction"] = "SavedRecipes";

            return View("Index", model);
        }

        private async Task<string> SaveImageAsync(IFormFile file)
        {
            var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "recipes");
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
            var filePath = Path.Combine(uploadsFolder, fileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(fileStream);
            }

            return $"/images/recipes/{fileName}";
        }

        private HtmlSanitizer GetHtmlSanitizer()
        {
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
            return sanitizer;
        }

        private void SanitizeRecipeContent(Recipe recipe)
        {
            var sanitizer = GetHtmlSanitizer();
            recipe.Description = sanitizer.Sanitize(recipe.Description);
            recipe.Instructions = sanitizer.Sanitize(recipe.Instructions);
        }

        private async Task<List<RecipeIngredient>> ProcessIngredientsAsync(string selectedIngredientsJson, int recipeId)
        {
            var selectedIngredients = string.IsNullOrEmpty(selectedIngredientsJson)
                ? new List<RecipeIngredientViewModel>()
                : JsonConvert.DeserializeObject<List<RecipeIngredientViewModel>>(selectedIngredientsJson);

            var recipeIngredients = new List<RecipeIngredient>();

            var defaultCategory = await _context.IngredientCategories
                .FirstOrDefaultAsync(u => u.Name == adddedByUserIngredientName);

            if (defaultCategory == null)
                throw new InvalidOperationException("Default ingredient category not found.");

            foreach (var item in selectedIngredients)
            {
                int ingredientId;

                var validationContext = new ValidationContext(item);
                Validator.ValidateObject(item, validationContext, validateAllProperties: true);

                if (item.IsNew)
                {
                    var existingIngredient = await _context.Ingredients
                        .FirstOrDefaultAsync(i => i.Name.ToLower() == item.Name.ToLower() && i.IngredientCategoryId == defaultCategory.Id);

                    if (existingIngredient != null)
                    {
                        ingredientId = existingIngredient.Id;
                    }
                    else
                    {
                        var newIngredient = new Ingredient
                        {
                            Name = item.Name,
                            IngredientCategoryId = defaultCategory.Id
                        };

                        _context.Ingredients.Add(newIngredient);
                        await _context.SaveChangesAsync();
                        ingredientId = newIngredient.Id;
                    }
                }
                else
                {
                    ingredientId = item.IngredientId;
                }

                var recipeIngredient = new RecipeIngredient
                {
                    IngredientId = ingredientId,
                    QuantityNeeded = item.QuantityNeeded,
                    RecipeId = recipeId
                };

                recipeIngredients.Add(recipeIngredient);
            }

            return recipeIngredients;
        }

    }
}
