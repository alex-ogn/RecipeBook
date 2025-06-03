using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RecipeBook.Data;
using RecipeBook.Models;
using RecipeBook.Services;
using RecipeBook.ViewModels.Users;
using System.Net.NetworkInformation;
using System.Security.Claims;

namespace RecipeBook.Controllers
{

    public class UsersController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly IUserProfileService _userProfileService;

        public UsersController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IUserProfileService userProfileService)
        {
            _context = context;
            _userManager = userManager;
            _userProfileService = userProfileService;
        }

        [Authorize]
        // Показва списък с всички потребители
        public async Task<IActionResult> Index(string search)
        {
            var usersQuery = _userManager.Users.AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                usersQuery = usersQuery.Where(u =>
                    u.UserName.Contains(search) || u.Email.Contains(search));
            }

            var users = await usersQuery.ToListAsync();
            ViewData["Search"] = search;
            return View(users);
        }


        [Authorize(Roles = "Admin")]
        // GET: Users/Edit/id
        public async Task<IActionResult> Edit(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var model = new EditUserViewModel
            {
                Id = user.Id,
                UserName = user.UserName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                ProfilePictureVersion = user.ProfilePictureVersion
            };

            return View(model);
        }


        // POST: Users/Edit/id
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditUserViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _userManager.FindByIdAsync(model.Id);
            if (user == null) return NotFound();

            var result = await _userProfileService.UpdateUserProfileAsync(user, model, isAdmin: true);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                    ModelState.AddModelError("", error.Description);

                return View(model);
            }

            TempData["SuccessMessage"] = "Профилът е обновен.";
            return RedirectToAction("Index");
        }


        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ChangePassword(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            ViewBag.UserId = user.Id;
            ViewBag.Username = user.UserName;
            return View();
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(string id, string newPassword)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, token, newPassword);

            if (result.Succeeded)
                return RedirectToAction(nameof(Index));

            foreach (var error in result.Errors)
                ModelState.AddModelError("", error.Description);

            return View();
        }

        [Authorize]
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