
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RecipeBook.Data;
using RecipeBook.ViewModels.Users;
using System.Security.Claims;


namespace RecipeBook.ViewComponents
{
    public class ProfileImageViewComponent : ViewComponent
    {
        private readonly ApplicationDbContext _context;

        public ProfileImageViewComponent(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            if (!User.Identity.IsAuthenticated)
                return View(null);

            var userId = ((ClaimsPrincipal)User).FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
                return View(null);

            return View(new ProfileImageViewModel
            {
                UserId = user.Id,
                ProfilePictureVersion = user.ProfilePictureVersion
            });
        }
    }

}
