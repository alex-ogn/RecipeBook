using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RecipeBook.Data;
using RecipeBook.Models;
using RecipeBook.Views.Home.ViewModels;
using RecipeBook.Views.Recipes.ViewModels;
using System.Diagnostics;
using System.Reflection;
using System.Text.RegularExpressions;

namespace RecipeBook.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public IActionResult Index()
        {
            var recipes = _context.Recipies
                .Include(r => r.Likes)
                .Include(r => r.User)
                .OrderByDescending(r => r.Likes.Count)  // Сортиране първо по харесвания
                .ThenByDescending(r => r.ViewCount)     // после по гледания
                .Take(6)
                .ToList();

            var cardViewModels = recipes.Select(r => new RecipeCardViewModel
            {
                Id = r.Id,
                Title = r.Title,
                DescriptionPreview = Regex.Replace(r.Description ?? "", "<.*?>", "")
                    .Substring(0, Math.Min(100, Regex.Replace(r.Description ?? "", "<.*?>", "").Length)) + "...",
                ImageUrl = Url.Action("GetImage", "Recipes", new { id = r.Id }),
                UserName = r.User?.UserName ?? "Потребител",
                UserId = r.UserId,
                LikesCount = r.Likes?.Count ?? 0,
                ViewCount = r.ViewCount,
                IsOwner = false
            }).ToList();

            var model = new HomeViewModel
            {
                CountUsers = _context.Users.Count(),
                CountRecipes = _context.Recipies.Count(),
                FeaturedRecipes = cardViewModels
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