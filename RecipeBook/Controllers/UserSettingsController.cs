using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using RecipeBook.Models;
using System.Net.NetworkInformation;

namespace RecipeBook.Controllers
{
    [Authorize]
    public class UserSettingsController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public UserSettingsController(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            return View(user);
        }

        [HttpPost]
        public async Task<IActionResult> UploadProfilePicture(IFormFile profilePicture)
        {
            var user = await _userManager.GetUserAsync(User);

            if (profilePicture != null && profilePicture.Length > 0)
            {
                using var memoryStream = new MemoryStream();
                await profilePicture.CopyToAsync(memoryStream);
                user.ProfilePicture = memoryStream.ToArray();

                await _userManager.UpdateAsync(user);
            }

            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> GetProfilePicture()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user?.ProfilePicture != null)
            {
                return File(user.ProfilePicture, "png");
            }

            return File("~/images/default-profile.png", "image/png");
        }

        [HttpGet]
        [Authorize]
        [Route("/Identity/Account/Manage/GetProfilePicture")]
        public async Task<IActionResult> GetProfilePicture([FromServices] UserManager<ApplicationUser> userManager)
        {
            var user = await userManager.GetUserAsync(User);
            if (user?.ProfilePicture != null)
            {
                return File(user.ProfilePicture, "image/jpg");
            }

            return File("~/images/default-profile", "image/png");
        }
    }
}