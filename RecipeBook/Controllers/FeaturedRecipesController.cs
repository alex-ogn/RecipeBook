using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RecipeBook.Constants;
using RecipeBook.Extensions;
using RecipeBook.Resources;
using RecipeBook.Services;
using RecipeBook.ViewModels.Recipes;

namespace RecipeBook.Controllers;

/// <summary>
/// Admin: which recipes the home page banner shows as its background slideshow.
/// Recipes are added from their details page and listed (and removed) here.
/// </summary>
[Authorize(Roles = UserRoles.Admin)]
public class FeaturedRecipesController : Controller
{
    private readonly IRecipeService _recipeService;

    public FeaturedRecipesController(IRecipeService recipeService)
    {
        _recipeService = recipeService;
    }

    public async Task<IActionResult> Index()
    {
        var featured = await _recipeService.GetFeaturedAsync();
        return View(RecipeCardMapper.ToCards(featured, currentUserId: null));
    }

    [HttpPost]
    public Task<IActionResult> Add(int id) => SetFeaturedAsync(id, featured: true);

    [HttpPost]
    public Task<IActionResult> Remove(int id) => SetFeaturedAsync(id, featured: false);

    private async Task<IActionResult> SetFeaturedAsync(int id, bool featured)
    {
        switch (await _recipeService.SetFeaturedAsync(id, featured))
        {
            case FeatureResult.NotFound:
                return NotFound();
            case FeatureResult.NoImage:
                TempData[TempDataKeys.ErrorMessage] = MessageTexts.RecipeFeatureNeedsImage;
                break;
            default:
                TempData[TempDataKeys.SuccessMessage] = featured ? MessageTexts.RecipeFeatured : MessageTexts.RecipeUnfeatured;
                break;
        }

        // Back to the recipe page or this list, whichever the admin came from.
        return this.RedirectToLocalReferer(Url.Action(nameof(Index))!);
    }
}
