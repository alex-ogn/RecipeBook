using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RecipeBook.Constants;
using RecipeBook.Models;
using RecipeBook.Resources;
using RecipeBook.Services;

namespace RecipeBook.Controllers
{
    [Authorize(Roles = UserRoles.Admin)]
    public class RecipeCategoriesController : Controller
    {
        private readonly IRecipeCategoryService _categoryService;

        public RecipeCategoriesController(IRecipeCategoryService categoryService)
        {
            _categoryService = categoryService;
        }

        // GET: RecipeCategories
        public async Task<IActionResult> Index()
        {
            return View(await _categoryService.GetAllAsync());
        }

        // GET: RecipeCategories/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            return await CategoryViewAsync(id);
        }

        // GET: RecipeCategories/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: RecipeCategories/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Name")] RecipeCategory recipeCategory)
        {
            if (await _categoryService.NameExistsAsync(recipeCategory.Name))
            {
                ModelState.AddModelError(nameof(RecipeCategory.Name), MessageTexts.CategoryNameExists);
            }

            if (!ModelState.IsValid)
            {
                return View(recipeCategory);
            }

            await _categoryService.CreateAsync(recipeCategory);
            return RedirectToAction(nameof(Index));
        }

        // GET: RecipeCategories/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            return await CategoryViewAsync(id);
        }

        // POST: RecipeCategories/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name")] RecipeCategory recipeCategory)
        {
            if (id != recipeCategory.Id)
            {
                return NotFound();
            }

            if (await _categoryService.NameExistsAsync(recipeCategory.Name, excludeId: recipeCategory.Id))
            {
                ModelState.AddModelError(nameof(RecipeCategory.Name), MessageTexts.CategoryNameExists);
            }

            if (!ModelState.IsValid)
            {
                return View(recipeCategory);
            }

            if (!await _categoryService.UpdateAsync(recipeCategory))
            {
                return NotFound();
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: RecipeCategories/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            return await CategoryViewAsync(id);
        }

        // POST: RecipeCategories/Delete/5
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
                    TempData[TempDataKeys.ErrorMessage] = string.Format(MessageTexts.RecipeCategoryInUse, string.Join(", ", result.UsedBy));
                    break;
                default:
                    TempData[TempDataKeys.SuccessMessage] = MessageTexts.RecipeCategoryDeleted;
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
