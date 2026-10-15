using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RecipeBook.Services;
using RecipeBook.Constants;

namespace RecipeBook.Controllers
{
    [Authorize(Roles = UserRoles.Admin)]
    public class AdminStatisticsController : Controller
    {
        private readonly IStatisticsService _statisticsService;

        public AdminStatisticsController(IStatisticsService statisticsService)
        {
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
            var model = await _statisticsService.GetRecipeStatisticsAsync();
            return View(model);
        }
    }

}
