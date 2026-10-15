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
using RecipeBook.Authorization;
using RecipeBook.Data;
using RecipeBook.Models;
using RecipeBook.Services;
using Ganss.Xss;
using RecipeBook.ViewModels.Recipes;
using RecipeBook.ViewModels.Enums;
using System.ComponentModel.DataAnnotations;
using RecipeBook.Constants;
using RecipeBook.Resources;

namespace RecipeBook.Controllers
{
    public class RecipesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IRecipePdfService _pdfService;
        private readonly IRecipeService _recipeService;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IImageStorageService _imageStorage;
        private readonly ILogger<RecipesController> _logger;

        public RecipesController(
            ApplicationDbContext context,
            IRecipePdfService pdfService,
            IRecipeService recipeService,
            IWebHostEnvironment webHostEnvironment,
            IImageStorageService imageStorage,
            ILogger<RecipesController> logger)
        {
            _context = context;
            _pdfService = pdfService;
            _recipeService = recipeService;
            _webHostEnvironment = webHostEnvironment;
            _imageStorage = imageStorage;
            _logger = logger;
        }

        public async Task<IActionResult> Index(int? SelectedCategoryId, RecipeSortOption sortOrder = RecipeSortOption.Newest)
        {
            string currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            ViewData[ViewDataKeys.CurrentUserId] = currentUserId;

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

            ViewData[ViewDataKeys.User] = user;
            ViewData[ViewDataKeys.CurrentUserId] = currentUserId;
            ViewData[ViewDataKeys.IsFollowing] = isFollowing;
            ViewData[ViewDataKeys.FollowersCount] = followersCount;
            ViewData[ViewDataKeys.FollowingCount] = followingCount;

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

            // Count the view once per cookie lifetime; the author's own views are not counted.
            var viewedCookie = CookieConstants.ViewedRecipePrefix + id;
            if (!Request.Cookies.ContainsKey(viewedCookie)
                && await _recipeService.RegisterViewAsync(recipe, currentUserId))
            {
                Response.Cookies.Append(viewedCookie, "1", new CookieOptions
                {
                    Expires = DateTimeOffset.UtcNow.Add(CookieConstants.ViewedRecipeLifetime),
                    HttpOnly = true,
                    SameSite = SameSiteMode.Lax
                });
            }

            bool isSaved = recipe.SavedByUsers
                                 .Any(s => s.UserId == currentUserId);

            RecipeViewModel recipeViewModel = new RecipeViewModel(recipe, similarRecipes);

            bool isLiked = await _context.RecipeLikes
                .AnyAsync(l => l.RecipeId == id && l.UserId == currentUserId);

            ViewData[ViewDataKeys.IsLiked] = isLiked;
            ViewData[ViewDataKeys.LikeCount] = await _context.RecipeLikes.CountAsync(l => l.RecipeId == id);

            bool isUserAdmin = User.IsInRole(UserRoles.Admin);
            ViewData[ViewDataKeys.IsUserAdmin] = isUserAdmin;
            ViewData[ViewDataKeys.CurrentUserId] = currentUserId;
            ViewData[ViewDataKeys.IsSaved] = isSaved;

            return View(recipeViewModel);
        }

        [Authorize(Roles = UserRoles.Admin + "," + UserRoles.User)]
        public IActionResult Create()
        {
            var viewModel = new RecipeEditViewModel
            {
                Categories = new SelectList(_context.RecipeCategories, "Id", "Name")
            };

            ViewData[ViewDataKeys.IsUserAdmin] = User.IsInRole(UserRoles.Admin);
            ViewData[ViewDataKeys.CurrentUserId] = User.FindFirstValue(ClaimTypes.NameIdentifier);

            return View(viewModel);
        }


        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(RecipeEditViewModel model, string SelectedIngredientsJson)
        {
            // The image URL is always set by the server, never taken from the form.
            model.ImageUrl = null;
            PrepareRecipeFormValidation(model);

            if (!ModelState.IsValid)
            {
                return RecipeFormView(model);
            }

            string? savedImageUrl = null;
            try
            {
                var recipe = new Recipe();
                model.EditRecipe(recipe);
                SanitizeRecipeContent(recipe);

                recipe.UserId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
                recipe.RecipeCategory = await _context.RecipeCategories.FindAsync(model.CategoryId!.Value);

                // Обработка на съставките
                recipe.RecipeIngredients = await ProcessIngredientsAsync(SelectedIngredientsJson, recipe.Id);

                if (HasUploadedFile(model.imageFileNew))
                {
                    savedImageUrl = await _imageStorage.SaveRecipeImageAsync(model.imageFileNew!);
                    recipe.ImageUrl = savedImageUrl;
                }

                _context.Recipies.Add(recipe);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }
            catch (ValidationException vex)
            {
                _imageStorage.DeleteRecipeImage(savedImageUrl);
                ModelState.AddModelError(string.Empty, vex.Message);
                return RecipeFormView(model);
            }
            catch (DbUpdateException ex)
            {
                _imageStorage.DeleteRecipeImage(savedImageUrl);
                _logger.LogError(ex, "Failed to create recipe for user {UserId}", User.FindFirstValue(ClaimTypes.NameIdentifier));
                ModelState.AddModelError(string.Empty, MessageTexts.SaveFailed);
                return RecipeFormView(model);
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

            if (!RecipePermissions.CanModify(recipe.UserId, User))
            {
                return Forbid();
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

            return View(viewModel);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, RecipeEditViewModel model, string SelectedIngredientsJson)
        {
            if (id != model.Id)
                return NotFound();

            var recipe = await _context.Recipies.FindAsync(id);
            if (recipe == null)
                return NotFound();

            if (!RecipePermissions.CanModify(recipe.UserId, User))
                return Forbid();

            // The image URL always comes from the database, never from the form.
            model.ImageUrl = recipe.ImageUrl;
            PrepareRecipeFormValidation(model);

            if (!ModelState.IsValid)
            {
                return RecipeFormView(model);
            }

            var oldImageUrl = recipe.ImageUrl;
            string? newImageUrl = null;
            try
            {
                model.EditRecipe(recipe);
                SanitizeRecipeContent(recipe);

                recipe.RecipeCategory = await _context.RecipeCategories.FindAsync(model.CategoryId!.Value);

                // Обработка на съставките
                var recipeIngredients = await ProcessIngredientsAsync(SelectedIngredientsJson, recipe.Id);

                // Изчистваме старите съставки
                var existing = await _context.RecipeIngredients
                    .Where(ri => ri.RecipeId == recipe.Id)
                    .ToListAsync();

                _context.RecipeIngredients.RemoveRange(existing);
                _context.RecipeIngredients.AddRange(recipeIngredients);

                if (HasUploadedFile(model.imageFileNew))
                {
                    newImageUrl = await _imageStorage.SaveRecipeImageAsync(model.imageFileNew!);
                    recipe.ImageUrl = newImageUrl;
                }

                await _context.SaveChangesAsync();

                // The new image is saved, so the old file is no longer referenced.
                if (newImageUrl != null)
                {
                    _imageStorage.DeleteRecipeImage(oldImageUrl);
                }

                return RedirectToAction(nameof(Index));
            }
            catch (ValidationException vex)
            {
                _imageStorage.DeleteRecipeImage(newImageUrl);
                ModelState.AddModelError(string.Empty, vex.Message);
                return RecipeFormView(model);
            }
            catch (DbUpdateException ex)
            {
                _imageStorage.DeleteRecipeImage(newImageUrl);
                _logger.LogError(ex, "Failed to update recipe {RecipeId}", id);
                ModelState.AddModelError(string.Empty, MessageTexts.SaveFailed);
                return RecipeFormView(model);
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

            if (!RecipePermissions.CanModify(recipe.UserId, User))
            {
                return Forbid();
            }

            return View(recipe);
        }

        // POST: Recipes/Delete/5
        [Authorize]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var success = await _recipeService.DeleteRecipeAsync(id, currentUserId, forceDelete: User.IsInRole(UserRoles.Admin));

            if (!success)
                return Forbid();

            TempData[TempDataKeys.SuccessMessage] = MessageTexts.RecipeDeleted;
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

            return RedirectToAction(nameof(Details), new { id });
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

            return RedirectToAction(nameof(Details), new { id });
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

            ViewData[ViewDataKeys.IsUserAdmin] = User.IsInRole(UserRoles.Admin);
            ViewData[ViewDataKeys.CurrentUserId] = userId;

            return View(nameof(Index), model);
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

            ViewData[ViewDataKeys.IsUserAdmin] = User.IsInRole(UserRoles.Admin);
            ViewData[ViewDataKeys.CurrentUserId] = userId;

            return View(nameof(Index), model);
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

            return File(pdf, MediaTypeNames.Application.Pdf, $"{recipe.Title}.pdf");
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddComment(int recipeId, string content)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(content)) return RedirectToAction(nameof(Details), new { id = recipeId });

            var comment = new RecipeComment
            {
                Content = content,
                RecipeId = recipeId,
                UserId = userId,
                CreatedAt = DateTime.UtcNow
            };

            _context.RecipeComments.Add(comment);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Details), new { id = recipeId });
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

            return RedirectToAction(nameof(Details), new { id = comment.RecipeId });
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
            return RedirectToAction(nameof(Details), new { id = comment.RecipeId });
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

            ViewData[ViewDataKeys.IsUserAdmin] = User.IsInRole(UserRoles.Admin);
            ViewData[ViewDataKeys.CurrentUserId] = userId;

            return View(nameof(Index), model);
        }

        /// <summary>
        /// Shared validation for the create and edit forms.
        /// </summary>
        private void PrepareRecipeFormValidation(RecipeEditViewModel model)
        {
            // Set by the server or only used to render the form, so they must not fail validation.
            ModelState.Remove(nameof(RecipeEditViewModel.ImageUrl));
            ModelState.Remove(nameof(RecipeEditViewModel.imageFileNew));
            ModelState.Remove(nameof(RecipeEditViewModel.Categories));

            if (model.CategoryId == null)
            {
                ModelState.AddModelError(nameof(RecipeEditViewModel.CategoryId),
                    string.Format(ValidationTexts.FieldRequired, DisplayTexts.Category));
            }

            if (HasUploadedFile(model.imageFileNew))
            {
                var imageError = _imageStorage.Validate(model.imageFileNew);
                if (imageError != null)
                {
                    ModelState.AddModelError(nameof(RecipeEditViewModel.imageFileNew), imageError);
                }
            }
        }

        private IActionResult RecipeFormView(RecipeEditViewModel model)
        {
            model.Categories = new SelectList(_context.RecipeCategories, "Id", "Name", model.CategoryId);
            ViewData[ViewDataKeys.CurrentUserId] = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return View(model);
        }

        private static bool HasUploadedFile(IFormFile? file) => file != null && file.Length > 0;

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
                .FirstOrDefaultAsync(u => u.Name == IngredientCategoryConstants.AddedByUserCategoryName);

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
