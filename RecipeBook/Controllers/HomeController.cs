using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RecipeBook.Models;
using RecipeBook.Services;
using RecipeBook.ViewModels.Home;
using RecipeBook.ViewModels.Recipes;
using System.Diagnostics;
using System.Security.Claims;

namespace RecipeBook.Controllers
{
    public class HomeController : Controller
    {
        private readonly IRecipeService _recipeService;
        private readonly UserManager<ApplicationUser> _userManager;
        private const int FeaturedRecipesCount = 6;

        public HomeController(IRecipeService recipeService, UserManager<ApplicationUser> userManager)
        {
            _recipeService = recipeService;
            _userManager = userManager;
        }

        public async Task<IActionResult> IndexAsync()
        {
            // Най-харесваните, после най-гледаните
            var recipes = await _recipeService.GetMostPopularAsync(FeaturedRecipesCount);

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var suggestions = userId == null
                ? new List<RecipeCardViewModel>()
                : await _recipeService.GetSuggestedRecipesForUserAsync(userId);

            var model = new HomeViewModel
            {
                CountUsers = await _userManager.Users.CountAsync(),
                CountRecipes = await _recipeService.CountAsync(),
                FeaturedRecipes = RecipeCardMapper.ToCards(recipes, currentUserId: null),
                Suggestions = suggestions
            };

            return View(model);
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