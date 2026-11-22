using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RecipeBook.Constants;
using RecipeBook.Models;
using RecipeBook.Services;
using RecipeBook.ViewModels.Enums;
using RecipeBook.ViewModels.Home;
using RecipeBook.ViewModels.Recipes;
using System.Diagnostics;
using System.Security.Claims;

namespace RecipeBook.Controllers
{
    public class HomeController : Controller
    {
        private readonly IRecipeService _recipeService;
        private readonly IRecipeCategoryService _categoryService;
        private readonly UserManager<ApplicationUser> _userManager;
        private const int PopularRecipesCount = 6;
        private const int NewestRecipesCount = 6;
        // The categories row scrolls sideways, so it can hold many; this only guards against an endless row.
        private const int CategoryTilesCount = 24;

        public HomeController(
            IRecipeService recipeService,
            IRecipeCategoryService categoryService,
            UserManager<ApplicationUser> userManager)
        {
            _recipeService = recipeService;
            _categoryService = categoryService;
            _userManager = userManager;
        }

        public async Task<IActionResult> IndexAsync()
        {
            // Най-харесваните, после най-гледаните
            var popular = await _recipeService.GetMostPopularAsync(PopularRecipesCount);

            var newest = await _recipeService.GetRecipeCardsAsync(
                new RecipeListFilter { Sort = RecipeSortOption.Newest }, page: 1, NewestRecipesCount);

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var suggestions = userId == null
                ? new List<RecipeCardViewModel>()
                : await _recipeService.GetSuggestedRecipesForUserAsync(userId);

            var model = new HomeViewModel
            {
                CountUsers = await _userManager.Users.CountAsync(),
                CountRecipes = await _recipeService.CountAsync(),
                HeroSlides = RecipeCardMapper.ToCards(await GetHeroSlidesAsync(), currentUserId: null),
                PopularRecipes = RecipeCardMapper.ToCards(popular, currentUserId: null),
                NewestRecipes = RecipeCardMapper.ToCards(newest, currentUserId: null),
                Suggestions = suggestions,
                Categories = await _categoryService.GetWithRecipeCountsAsync(CategoryTilesCount),
                IsSignedIn = userId != null
            };

            return View(model);
        }

        /// <summary>
        /// Photos for the banner slideshow: the recipes an admin featured, or, while there are none,
        /// the most liked recipes that have a photo.
        /// </summary>
        private async Task<IEnumerable<RecipeCardSource>> GetHeroSlidesAsync()
        {
            // A featured recipe whose photo was removed later is skipped, not shown with the default image.
            var featured = (await _recipeService.GetFeaturedAsync())
                .Where(r => !string.IsNullOrWhiteSpace(r.ImageUrl))
                .ToList();
            if (featured.Count > 0)
            {
                return featured;
            }

            return await _recipeService.GetRecipeCardsAsync(
                new RecipeListFilter { HasImage = true, Sort = RecipeSortOption.MostLiked },
                page: 1,
                FeaturedRecipeConstants.DefaultCount);
        }


        public IActionResult About()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}