using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RecipeBook.Data;
using RecipeBook.Services;
using RecipeBook.ViewModels.AdminStatistics;
using System.Data;

namespace RecipeBook.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminStatisticsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IStatisticsService _statisticsService;

        public AdminStatisticsController(ApplicationDbContext context, IStatisticsService statisticsService)
        {
            _context = context;
            _statisticsService = statisticsService;
        }

        [HttpGet]
        public async Task<IActionResult> UserStats()
        {
            var result = await _statisticsService.GetUserStatisticsAsync();
            return View(result);
        }

        [HttpGet]
        public async Task<IActionResult> RecipeStats()
        {
            var model = await _statisticsService.GetRecipeStatsAsync();
            return View(model);
        }
    }

}
