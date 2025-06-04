using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using RecipeBook.Data;
using RecipeBook.ViewModels.Recipes;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

public class SearchController : Controller
{
    private readonly ApplicationDbContext _context;

    public SearchController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(string searchString)
    {
        if (string.IsNullOrWhiteSpace(searchString))
        {
            return RedirectToAction("Index", "Recipes");
        }

        searchString = searchString.ToLower().Trim();

        // Търсене в потребители
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.UserName.ToLower().Contains(searchString));

        if (user != null)
        {
            return RedirectToAction("MyIndex", "Recipes", new { id = user.Id });
        }

        // Търсене в категории
        var category = await _context.RecipeCategories
            .FirstOrDefaultAsync(c => c.Name.ToLower().Contains(searchString));

        if (category != null)
        {
            return RedirectToAction("Index", "Recipes", new { categoryId = category.Id });
        }

        // Търсене по име на рецепта
        var matchingRecipes = await _context.Recipies
            .Include(r => r.User)
            .Include(r => r.RecipeCategory)
            .Where(r => r.Title.ToLower().Contains(searchString))
            .ToListAsync();

        // Създай view model и пренасочи
        TempData["SearchResults"] = searchString; // по желание за съобщение
        ViewData["CurrentUserId"] = User.FindFirstValue(ClaimTypes.NameIdentifier);
        ViewData["IsUserAdmin"] = User.IsInRole("Admin");

        var model = new RecipeFilterViewModel
        {
            Recipes = matchingRecipes
                .Select(r => new RecipeCardViewModel
                {
                    Id = r.Id,
                    Title = r.Title,
                    DescriptionPreview = r.Description,
                    ImageUrl = Url.Action("GetImage", "Recipes", new { id = r.Id }),
                    UserName = r.User.UserName,
                    UserId = r.UserId,
                    LikesCount = r.Likes?.Count ?? 0,
                    ViewCount = r.ViewCount,
                    IsOwner = r.UserId == User.FindFirstValue(ClaimTypes.NameIdentifier)
                }).ToList(),
            Categories = new SelectList(_context.RecipeCategories, "Id", "Name"),
            SelectedCategoryId = null
        };

        return View("~/Views/Recipes/Index.cshtml", model);
    }


}
