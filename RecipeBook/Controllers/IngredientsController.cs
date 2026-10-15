using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using RecipeBook.Constants;
using RecipeBook.Models;
using RecipeBook.Resources;
using RecipeBook.Services;

namespace RecipeBook.Controllers
{
    [Authorize(Roles = UserRoles.Admin)]
    public class IngredientsController : Controller
    {
        private readonly IIngredientService _ingredientService;
        private readonly IIngredientCategoryService _categoryService;

        public IngredientsController(IIngredientService ingredientService, IIngredientCategoryService categoryService)
        {
            _ingredientService = ingredientService;
            _categoryService = categoryService;
        }

        public async Task<IActionResult> Index(string searchString, int? categoryId)
        {
            ViewData[ViewDataKeys.Categories] = new SelectList(await _categoryService.GetAllAsync(), "Id", "Name");
            ViewData[ViewDataKeys.Search] = searchString;
            ViewData[ViewDataKeys.SelectedCategoryId] = categoryId;

            return View(await _ingredientService.SearchAsync(searchString, categoryId));
        }

        public async Task<IActionResult> Details(int? id)
        {
            return await IngredientViewAsync(id);
        }

        public async Task<IActionResult> Create()
        {
            return await IngredientFormViewAsync(null);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Name,IngredientCategoryId")] Ingredient ingredient)
        {
            if (await _ingredientService.ExistsInCategoryAsync(ingredient.Name, ingredient.IngredientCategoryId))
            {
                ModelState.AddModelError(string.Empty, MessageTexts.IngredientNameExists);
            }

            if (!ModelState.IsValid)
            {
                return await IngredientFormViewAsync(ingredient);
            }

            await _ingredientService.CreateAsync(ingredient);
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var ingredient = await _ingredientService.GetByIdAsync(id.Value);
            return ingredient == null ? NotFound() : await IngredientFormViewAsync(ingredient);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,IngredientCategoryId")] Ingredient ingredient)
        {
            if (id != ingredient.Id)
            {
                return NotFound();
            }

            if (await _ingredientService.ExistsInCategoryAsync(ingredient.Name, ingredient.IngredientCategoryId, excludeId: ingredient.Id))
            {
                ModelState.AddModelError(string.Empty, MessageTexts.IngredientNameExists);
            }

            if (!ModelState.IsValid)
            {
                return await IngredientFormViewAsync(ingredient);
            }

            if (!await _ingredientService.UpdateAsync(ingredient))
            {
                return NotFound();
            }

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int? id)
        {
            return await IngredientViewAsync(id);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var result = await _ingredientService.DeleteAsync(id);

            switch (result.Status)
            {
                case DeleteStatus.NotFound:
                    return NotFound();
                case DeleteStatus.InUse:
                    TempData[TempDataKeys.ErrorMessage] = string.Format(MessageTexts.IngredientInUse, string.Join(", ", result.UsedBy));
                    break;
                default:
                    TempData[TempDataKeys.SuccessMessage] = MessageTexts.IngredientDeleted;
                    break;
            }

            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// Ingredient list for the picker on the recipe create/edit forms.
        /// </summary>
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetAll()
        {
            return Json(await _ingredientService.GetOptionsAsync());
        }

        private async Task<IActionResult> IngredientViewAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var ingredient = await _ingredientService.GetByIdAsync(id.Value);
            return ingredient == null ? NotFound() : View(ingredient);
        }

        private async Task<IActionResult> IngredientFormViewAsync(Ingredient? ingredient)
        {
            ViewData[ViewDataKeys.IngredientCategories] = new SelectList(
                await _categoryService.GetAllAsync(), "Id", "Name", ingredient?.IngredientCategoryId);
            return View(ingredient);
        }
    }
}
