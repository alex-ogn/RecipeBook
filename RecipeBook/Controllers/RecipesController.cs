using System.ComponentModel.DataAnnotations;
using System.Net.Mime;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using RecipeBook.Authorization;
using RecipeBook.Constants;
using RecipeBook.Extensions;
using RecipeBook.Models;
using RecipeBook.Resources;
using RecipeBook.Services;
using RecipeBook.ViewModels.Enums;
using RecipeBook.ViewModels.Recipes;

namespace RecipeBook.Controllers
{
    public class RecipesController : Controller
    {
        private readonly IRecipeService _recipeService;
        private readonly IRecipeInteractionService _interactionService;
        private readonly IRecipeCategoryService _categoryService;
        private readonly IFollowService _followService;
        private readonly IRecipePdfService _pdfService;
        private readonly IImageStorageService _imageStorage;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly ILogger<RecipesController> _logger;

        public RecipesController(
            IRecipeService recipeService,
            IRecipeInteractionService interactionService,
            IRecipeCategoryService categoryService,
            IFollowService followService,
            IRecipePdfService pdfService,
            IImageStorageService imageStorage,
            UserManager<ApplicationUser> userManager,
            IWebHostEnvironment webHostEnvironment,
            ILogger<RecipesController> logger)
        {
            _recipeService = recipeService;
            _interactionService = interactionService;
            _categoryService = categoryService;
            _followService = followService;
            _pdfService = pdfService;
            _imageStorage = imageStorage;
            _userManager = userManager;
            _webHostEnvironment = webHostEnvironment;
            _logger = logger;
        }

        private string? CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier);

        public async Task<IActionResult> Index(int? SelectedCategoryId, RecipeSortOption sortOrder = RecipeSortOption.Newest)
        {
            var filter = new RecipeListFilter { CategoryId = SelectedCategoryId, Sort = sortOrder };
            return View(await BuildRecipeListAsync(filter, SelectedCategoryId));
        }

        public async Task<IActionResult> MyIndex(string? id)
        {
            // Ако id не е подадено, използваме текущия потребител
            var currentUserId = CurrentUserId;
            var userId = id ?? currentUserId;
            if (userId == null)
            {
                return Challenge();
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            var recipes = await _recipeService.GetRecipesAsync(new RecipeListFilter { AuthorId = userId });

            var model = new UserRecipesViewModel
            {
                User = user,
                Recipes = RecipeCardMapper.ToCards(recipes, currentUserId, User.IsInRole(UserRoles.Admin)),
                IsOwnProfile = user.Id == currentUserId,
                IsFollowing = await _followService.IsFollowingAsync(currentUserId, user.Id),
                FollowersCount = await _followService.CountFollowersAsync(user.Id),
                FollowingCount = await _followService.CountFollowingAsync(user.Id)
            };

            return View(model);
        }

        // GET: Recipes/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var currentUserId = CurrentUserId;
            var details = await _recipeService.GetDetailsAsync(id, currentUserId);
            if (details == null)
            {
                return NotFound();
            }

            // Count the view once per cookie lifetime; the author's own views are not counted.
            var viewedCookie = CookieConstants.ViewedRecipePrefix + id;
            if (!Request.Cookies.ContainsKey(viewedCookie)
                && await _recipeService.RegisterViewAsync(details.Recipe, currentUserId))
            {
                Response.Cookies.Append(viewedCookie, "1", new CookieOptions
                {
                    Expires = DateTimeOffset.UtcNow.Add(CookieConstants.ViewedRecipeLifetime),
                    HttpOnly = true,
                    SameSite = SameSiteMode.Lax
                });
            }

            var model = new RecipeViewModel(details.Recipe, details.SimilarRecipes)
            {
                CurrentUserId = currentUserId,
                IsUserAdmin = User.IsInRole(UserRoles.Admin),
                IsLiked = details.IsLiked,
                IsSaved = details.IsSaved,
                LikeCount = details.LikeCount,
                CanModify = RecipePermissions.CanModify(details.Recipe.UserId, User)
            };

            return View(model);
        }

        [Authorize(Roles = UserRoles.Admin + "," + UserRoles.User)]
        public async Task<IActionResult> Create()
        {
            var model = new RecipeEditViewModel
            {
                Categories = await CategorySelectListAsync(null)
            };

            return View(model);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(RecipeEditViewModel model, string? SelectedIngredientsJson)
        {
            // The image URL is always set by the server, never taken from the form.
            model.ImageUrl = null;
            var ingredients = ParseIngredients(SelectedIngredientsJson);
            PrepareRecipeFormValidation(model, ingredients);

            if (!ModelState.IsValid)
            {
                return await RecipeFormViewAsync(model, ingredients);
            }

            string? savedImageUrl = null;
            try
            {
                var recipe = new Recipe { UserId = CurrentUserId!, RecipeCategoryId = model.CategoryId!.Value };
                model.EditRecipe(recipe);

                if (HasUploadedFile(model.imageFileNew))
                {
                    savedImageUrl = await _imageStorage.SaveRecipeImageAsync(model.imageFileNew!);
                    recipe.ImageUrl = savedImageUrl;
                }

                await _recipeService.CreateAsync(recipe, ingredients!);
                return RedirectToAction(nameof(Index));
            }
            catch (ValidationException vex)
            {
                _imageStorage.DeleteRecipeImage(savedImageUrl);
                ModelState.AddModelError(string.Empty, vex.Message);
                return await RecipeFormViewAsync(model, ingredients);
            }
            catch (DbUpdateException ex)
            {
                _imageStorage.DeleteRecipeImage(savedImageUrl);
                _logger.LogError(ex, "Failed to create recipe for user {UserId}", CurrentUserId);
                ModelState.AddModelError(string.Empty, MessageTexts.SaveFailed);
                return await RecipeFormViewAsync(model, ingredients);
            }
        }

        [Authorize]
        // GET: Recipes/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var recipe = await _recipeService.GetForEditAsync(id.Value);
            if (recipe == null)
            {
                return NotFound();
            }

            if (!RecipePermissions.CanModify(recipe.UserId, User))
            {
                return Forbid();
            }

            var model = new RecipeEditViewModel(recipe)
            {
                SelectedIngredients = recipe.RecipeIngredients
                    .Select(ri => new RecipeIngredientViewModel
                    {
                        IngredientId = ri.IngredientId,
                        Name = ri.Ingredient.Name,
                        QuantityNeeded = ri.QuantityNeeded
                    }).ToList(),
                Categories = await CategorySelectListAsync(recipe.RecipeCategoryId)
            };

            return View(model);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, RecipeEditViewModel model, string? SelectedIngredientsJson)
        {
            if (id != model.Id)
                return NotFound();

            var recipe = await _recipeService.GetByIdAsync(id);
            if (recipe == null)
                return NotFound();

            if (!RecipePermissions.CanModify(recipe.UserId, User))
                return Forbid();

            // The image URL always comes from the database, never from the form.
            model.ImageUrl = recipe.ImageUrl;
            var ingredients = ParseIngredients(SelectedIngredientsJson);
            PrepareRecipeFormValidation(model, ingredients);

            if (!ModelState.IsValid)
            {
                return await RecipeFormViewAsync(model, ingredients);
            }

            var oldImageUrl = recipe.ImageUrl;
            string? newImageUrl = null;
            try
            {
                model.EditRecipe(recipe);
                recipe.RecipeCategoryId = model.CategoryId!.Value;

                if (HasUploadedFile(model.imageFileNew))
                {
                    newImageUrl = await _imageStorage.SaveRecipeImageAsync(model.imageFileNew!);
                    recipe.ImageUrl = newImageUrl;
                }

                await _recipeService.UpdateAsync(recipe, ingredients!);

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
                return await RecipeFormViewAsync(model, ingredients);
            }
            catch (DbUpdateException ex)
            {
                _imageStorage.DeleteRecipeImage(newImageUrl);
                _logger.LogError(ex, "Failed to update recipe {RecipeId}", id);
                ModelState.AddModelError(string.Empty, MessageTexts.SaveFailed);
                return await RecipeFormViewAsync(model, ingredients);
            }
        }

        [Authorize]
        // GET: Recipes/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var recipe = await _recipeService.GetWithAuthorAsync(id.Value);
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
            var success = await _recipeService.DeleteRecipeAsync(id, CurrentUserId, forceDelete: User.IsInRole(UserRoles.Admin));

            if (!success)
                return Forbid();

            TempData[TempDataKeys.SuccessMessage] = MessageTexts.RecipeDeleted;
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> SaveRecipe(int id)
        {
            if (!await _interactionService.SaveAsync(id, CurrentUserId!))
                return NotFound();

            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> UnsaveRecipe(int id)
        {
            await _interactionService.UnsaveAsync(id, CurrentUserId!);
            return RedirectToAction(nameof(Details), new { id });
        }

        [Authorize]
        public async Task<IActionResult> LikedRecipes(int? categoryId)
        {
            var filter = new RecipeListFilter { CategoryId = categoryId, LikedByUserId = CurrentUserId };
            return View(nameof(Index), await BuildRecipeListAsync(filter, categoryId));
        }

        [Authorize]
        public async Task<IActionResult> SavedRecipes(int? categoryId)
        {
            var filter = new RecipeListFilter { CategoryId = categoryId, SavedByUserId = CurrentUserId };
            return View(nameof(Index), await BuildRecipeListAsync(filter, categoryId));
        }

        [Authorize]
        public async Task<IActionResult> FollowedUsersRecipes(int? categoryId)
        {
            var filter = new RecipeListFilter { CategoryId = categoryId, FollowedByUserId = CurrentUserId };
            return View(nameof(Index), await BuildRecipeListAsync(filter, categoryId));
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Like(int id)
        {
            if (!await _interactionService.LikeAsync(id, CurrentUserId!))
                return NotFound();

            return this.RedirectToLocalReferer(DetailsUrl(id));
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Unlike(int id)
        {
            await _interactionService.UnlikeAsync(id, CurrentUserId!);
            return this.RedirectToLocalReferer(DetailsUrl(id));
        }

        [HttpGet]
        public async Task<IActionResult> ExportPdf(int id)
        {
            var recipe = await _recipeService.GetForPdfAsync(id);
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
            if (!string.IsNullOrWhiteSpace(content)
                && !await _interactionService.AddCommentAsync(recipeId, CurrentUserId!, content))
            {
                return NotFound();
            }

            return RedirectToAction(nameof(Details), new { id = recipeId });
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> DeleteComment(int id)
        {
            var comment = await _interactionService.GetCommentAsync(id);
            if (comment == null) return NotFound();

            if (!RecipePermissions.CanDeleteComment(comment.UserId, comment.Recipe.UserId, User))
                return Forbid();

            await _interactionService.DeleteCommentAsync(comment);
            return RedirectToAction(nameof(Details), new { id = comment.RecipeId });
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> EditComment(int id)
        {
            var comment = await _interactionService.GetCommentAsync(id);
            if (comment == null) return NotFound();

            if (!RecipePermissions.CanEditComment(comment.UserId, User))
                return Forbid();

            return View(new CommentEditViewModel { Id = comment.Id, Content = comment.Content });
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditComment(CommentEditViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var comment = await _interactionService.GetCommentAsync(model.Id);
            if (comment == null) return NotFound();

            if (!RecipePermissions.CanEditComment(comment.UserId, User))
                return Forbid();

            await _interactionService.UpdateCommentAsync(comment, model.Content);
            return RedirectToAction(nameof(Details), new { id = comment.RecipeId });
        }

        private async Task<RecipeFilterViewModel> BuildRecipeListAsync(RecipeListFilter filter, int? selectedCategoryId)
        {
            var recipes = await _recipeService.GetRecipesAsync(filter);

            return new RecipeFilterViewModel
            {
                Recipes = RecipeCardMapper.ToCards(recipes, CurrentUserId, User.IsInRole(UserRoles.Admin)),
                Categories = await CategorySelectListAsync(selectedCategoryId),
                SelectedCategoryId = selectedCategoryId,
                SortOrder = filter.Sort
            };
        }

        private async Task<SelectList> CategorySelectListAsync(int? selectedCategoryId)
            => new(await _categoryService.GetAllAsync(), nameof(RecipeCategory.Id), nameof(RecipeCategory.Name), selectedCategoryId);

        /// <summary>
        /// Reads the ingredient list posted by the recipe form's script. Returns null when the JSON is malformed.
        /// </summary>
        private static List<RecipeIngredientViewModel>? ParseIngredients(string? selectedIngredientsJson)
        {
            if (string.IsNullOrWhiteSpace(selectedIngredientsJson))
            {
                return new List<RecipeIngredientViewModel>();
            }

            try
            {
                return JsonConvert.DeserializeObject<List<RecipeIngredientViewModel>>(selectedIngredientsJson)
                    ?? new List<RecipeIngredientViewModel>();
            }
            catch (JsonException)
            {
                return null;
            }
        }

        /// <summary>
        /// Shared validation for the create and edit forms.
        /// </summary>
        private void PrepareRecipeFormValidation(RecipeEditViewModel model, List<RecipeIngredientViewModel>? ingredients)
        {
            // Set by the server or only used to render the form, so they must not fail validation.
            ModelState.Remove(nameof(RecipeEditViewModel.ImageUrl));
            ModelState.Remove(nameof(RecipeEditViewModel.imageFileNew));
            ModelState.Remove(nameof(RecipeEditViewModel.Categories));
            ModelState.Remove(nameof(RecipeEditViewModel.SelectedIngredients));

            if (model.CategoryId == null)
            {
                ModelState.AddModelError(nameof(RecipeEditViewModel.CategoryId),
                    string.Format(ValidationTexts.FieldRequired, DisplayTexts.Category));
            }

            if (ingredients == null)
            {
                ModelState.AddModelError(string.Empty, MessageTexts.SaveFailed);
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

        /// <summary>
        /// Redisplays the create/edit form, keeping the ingredients the user had picked.
        /// </summary>
        private async Task<IActionResult> RecipeFormViewAsync(RecipeEditViewModel model, List<RecipeIngredientViewModel>? ingredients)
        {
            model.Categories = await CategorySelectListAsync(model.CategoryId);
            if (ingredients != null)
            {
                model.SelectedIngredients = ingredients;
            }

            return View(model);
        }

        private string DetailsUrl(int id) => Url.Action(nameof(Details), new { id })!;

        private static bool HasUploadedFile(IFormFile? file) => file != null && file.Length > 0;
    }
}
