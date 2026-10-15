using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RecipeBook.Constants;
using RecipeBook.Models;
using RecipeBook.Resources;
using RecipeBook.Services;

namespace RecipeBook.Controllers
{
    [Authorize(Roles = UserRoles.Admin)]
    public class IngredientCategoriesController : Controller
    {
        private readonly IIngredientCategoryService _categoryService;

        public IngredientCategoriesController(IIngredientCategoryService categoryService)
        {
            _categoryService = categoryService;
        }

        // GET: IngredientCategories
        public async Task<IActionResult> Index()
        {
            return View(await _categoryService.GetAllAsync());
        }

        // GET: IngredientCategories/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            return await CategoryViewAsync(id);
        }

        // GET: IngredientCategories/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: IngredientCategories/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Name")] IngredientCategory ingredientCategory)
        {
            if (await _categoryService.NameExistsAsync(ingredientCategory.Name))
            {
                ModelState.AddModelError(nameof(IngredientCategory.Name), MessageTexts.CategoryNameExists);
            }

            if (!ModelState.IsValid)
            {
                return View(ingredientCategory);
            }

            await _categoryService.CreateAsync(ingredientCategory);
            return RedirectToAction(nameof(Index));
        }

        // GET: IngredientCategories/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            return await CategoryViewAsync(id);
        }

        // POST: IngredientCategories/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name")] IngredientCategory ingredientCategory)
        {
            if (id != ingredientCategory.Id)
            {
                return NotFound();
            }

            if (await _categoryService.NameExistsAsync(ingredientCategory.Name, excludeId: ingredientCategory.Id))
            {
                ModelState.AddModelError(nameof(IngredientCategory.Name), MessageTexts.CategoryNameExists);
            }

            if (!ModelState.IsValid)
            {
                return View(ingredientCategory);
            }

            if (!await _categoryService.UpdateAsync(ingredientCategory))
            {
                return NotFound();
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: IngredientCategories/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            return await CategoryViewAsync(id);
        }

        // POST: IngredientCategories/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var result = await _categoryService.DeleteAsync(id);

            switch (result.Status)
            {
                case DeleteStatus.NotFound:
                    return NotFound();
                case DeleteStatus.InUse:
                    TempData[TempDataKeys.ErrorMessage] = string.Format(MessageTexts.IngredientCategoryInUse, string.Join(", ", result.UsedBy));
                    break;
                default:
                    TempData[TempDataKeys.SuccessMessage] = MessageTexts.IngredientCategoryDeleted;
                    break;
            }

            return RedirectToAction(nameof(Index));
        }

        private async Task<IActionResult> CategoryViewAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var category = await _categoryService.GetByIdAsync(id.Value);
            return category == null ? NotFound() : View(category);
        }
    }
}
