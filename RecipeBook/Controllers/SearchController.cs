using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using RecipeBook.Constants;
using RecipeBook.Controllers;
using RecipeBook.Models;
using RecipeBook.Services;
using RecipeBook.ViewModels.Recipes;
using System.Security.Claims;

public class SearchController : Controller
{
    private readonly IRecipeService _recipeService;
    private readonly IRecipeCategoryService _categoryService;
    private readonly UserManager<ApplicationUser> _userManager;

    public SearchController(IRecipeService recipeService, IRecipeCategoryService categoryService, UserManager<ApplicationUser> userManager)
    {
        _recipeService = recipeService;
        _categoryService = categoryService;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index(string searchString)
    {
        if (string.IsNullOrWhiteSpace(searchString))
        {
            return RedirectToAction(nameof(RecipesController.Index), "Recipes");
        }

        searchString = searchString.ToLower().Trim();

        // Търсене в потребители
        var user = await _userManager.Users
            .FirstOrDefaultAsync(u => u.UserName!.ToLower().Contains(searchString));

        if (user != null)
        {
            return RedirectToAction(nameof(RecipesController.MyIndex), "Recipes", new { id = user.Id });
        }

        // Търсене в категории
        var category = await _categoryService.FindByNamePartAsync(searchString);
        if (category != null)
        {
            return RedirectToAction(nameof(RecipesController.Index), "Recipes", new { SelectedCategoryId = category.Id });
        }

        // Търсене по име на рецепта
        var matchingRecipes = await _recipeService.GetRecipesAsync(new RecipeListFilter { TitleContains = searchString });

        TempData[TempDataKeys.SearchResults] = searchString; // по желание за съобщение
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var model = new RecipeFilterViewModel
        {
            Recipes = RecipeCardMapper.ToCards(matchingRecipes, currentUserId, User.IsInRole(UserRoles.Admin)),
            Categories = new SelectList(await _categoryService.GetAllAsync(), nameof(RecipeCategory.Id), nameof(RecipeCategory.Name)),
            SelectedCategoryId = null
        };

        return View("~/Views/Recipes/Index.cshtml", model);
    }
}
