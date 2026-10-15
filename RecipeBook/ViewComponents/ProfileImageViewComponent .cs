using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using RecipeBook.Models;
using RecipeBook.ViewModels.Users;
using System.Security.Claims;


namespace RecipeBook.ViewComponents
{
    public class ProfileImageViewComponent : ViewComponent
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public ProfileImageViewComponent(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            if (User.Identity?.IsAuthenticated != true)
                return View(null);

            var user = await _userManager.GetUserAsync((ClaimsPrincipal)User);

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
