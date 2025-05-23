using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RecipeBook.Data;
using RecipeBook.Models;
using RecipeBook.Views.Users.ViewModels;
using System.Net.NetworkInformation;
using System.Security.Claims;

namespace RecipeBook.Controllers
{
    [Authorize]
    public class UsersController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;

        public UsersController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
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
        public async Task<IActionResult> GetProfilePicture(string userId)
        {
            var user = await _userManager.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null) return NotFound();

            if (user?.ProfilePicture != null)
            {
                return File(user.ProfilePicture, "image/jpg");
            }

            return File("~/images/default-profile.png", "image/png");
        }

        [HttpGet]
        [Authorize]
        [Route("/Identity/Account/Manage/GetCurrentUserProfilePicture")]
        public async Task<IActionResult> GetCurrentUserProfilePicture([FromServices] UserManager<ApplicationUser> userManager)
        {
            var user = await userManager.GetUserAsync(User);
            if (user?.ProfilePicture != null)
            {
                return File(user.ProfilePicture, "image/jpg");
            }

            return File("~/images/default-profile", "image/png");
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Follow(string userId)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (currentUserId == userId) return BadRequest();

            var exists = await _context.UserFollowers
                .AnyAsync(uf => uf.FollowerId == currentUserId && uf.FollowedId == userId);

            if (!exists)
            {
                _context.UserFollowers.Add(new UserFollower
                {
                    FollowerId = currentUserId,
                    FollowedId = userId
                });

                await _context.SaveChangesAsync();
            }

            // върни се към страницата, от която е подадена формата
            return Redirect(Request.Headers["Referer"].ToString());
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Unfollow(string userId)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var follow = await _context.UserFollowers
                .FirstOrDefaultAsync(uf => uf.FollowerId == currentUserId && uf.FollowedId == userId);

            if (follow != null)
            {
                _context.UserFollowers.Remove(follow);
                await _context.SaveChangesAsync();
            }

            // върни се към страницата, от която е подадена формата
            return Redirect(Request.Headers["Referer"].ToString());
        }

        [Authorize]
        public async Task<IActionResult> Followers(string id)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var followers = await _context.UserFollowers
                .Where(f => f.FollowedId == id)
                .Include(f => f.Follower)
                .ToListAsync();

            var viewModel = followers.Select(f => new FollowViewModel
            {
                User = f.Follower,
                IsFollowing = _context.UserFollowers.Any(ff => ff.FollowerId == currentUserId && ff.FollowedId == f.FollowerId)
            }).ToList();

            ViewData["ListTitle"] = "Последователи";
            ViewData["CurrentUserId"] = currentUserId;

            return View("FollowList", viewModel);
        }

        [Authorize]
        public async Task<IActionResult> Following(string id)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var following = await _context.UserFollowers
                .Where(f => f.FollowerId == id)
                .Include(f => f.Followed)
                .ToListAsync();

            var viewModel = following.Select(f => new FollowViewModel
            {
                User = f.Followed,
                IsFollowing = _context.UserFollowers.Any(ff => ff.FollowerId == currentUserId && ff.FollowedId == f.FollowedId)
            }).ToList();

            ViewData["ListTitle"] = "Следвани";
            ViewData["CurrentUserId"] = currentUserId;

            return View("FollowList", viewModel);
        }


    }
}