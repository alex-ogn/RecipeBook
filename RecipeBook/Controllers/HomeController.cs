using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RecipeBook.Data;
using RecipeBook.Models;
using RecipeBook.Views.Home.ViewModels;
using System.Diagnostics;
using System.Reflection;

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
            var model = new HomeViewModel
            {
                FeaturedRecipes = _context.Recipies.Take(6).ToList() // Fetches the first 6 recipes
            };

            //HomeViewModel model = new HomeViewModel();
            //model.FeaturedRecipes = new List<Recipe>
            //{
            //    new Recipe() { Title = "Some Title", Description = "Some Description" },
            //    new Recipe() { Title = "Some Title", Description = "Some Description" },
            //    new Recipe() { Title = "Some Title", Description = "Some Description" },
            //    new Recipe() { Title = "Some Title", Description = "Some Description" }
            //};

            return View(model);
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