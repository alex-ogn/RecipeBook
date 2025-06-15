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
        private readonly SignInManager<ApplicationUser> _signInManager;

        public UsersController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IUserProfileService userProfileService, SignInManager<ApplicationUser> signInManager)
        {
            _context = context;
            _userManager = userManager;
            _userProfileService = userProfileService;
            _signInManager = signInManager;
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
        [HttpGet]
        public async Task<IActionResult> Details(string? id)
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


        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Edit(string? id)
        {
            // Ако няма ID (т.е. обикновен потребител), вземаме текущия
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole("Admin");

            if (!isAdmin && id != null && id != currentUserId)
            {
                return Forbid(); // Не е админ, но пробва да редактира чужд профил
            }

            var targetId = id ?? currentUserId;
            var user = await _userManager.FindByIdAsync(targetId);
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
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditUserViewModel model)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole("Admin");

            // Ограничение: обикновен потребител може да променя само себе си
            if (!isAdmin && model.Id != currentUserId)
            {
                return Forbid();
            }

            if (!ModelState.IsValid) return View(model);

            var user = await _userManager.FindByIdAsync(model.Id);
            if (user == null) return NotFound();

            var result = await _userProfileService.UpdateUserProfileAsync(user, model, isAdmin);
            if (!result.Succeeded)
            {
                TempData["ErrorMessage"] = string.Join("; ", result.Errors.Select(e => e.Description));
                return View(model);
            }

            if (!isAdmin)
                await _signInManager.RefreshSignInAsync(user); // важи само за текущия юзър

            TempData["SuccessMessage"] = "Профилът е обновен.";
            return isAdmin ? RedirectToAction("Index") : RedirectToAction("Edit");
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
            var (content, contentType) = await _userProfileService.GetProfilePictureAsync(userId);
            return File(content, contentType);
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

        [HttpGet]
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

        [HttpGet]
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

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var result = await _userProfileService.DeleteUserAsync(id, currentUserId);

            if (!result.Succeeded)
            {
                string errorMsg = "Потребителят не беше изтрит.";

                foreach (var error in result.Errors)
                {
                    errorMsg += " " + error.Description;
                }

                TempData["ErrorMessage"] = errorMsg;
                return RedirectToAction("Edit", new { id });
            }

            TempData["SuccessMessage"] = "Потребителят беше успешно изтрит.";
            return RedirectToAction("Index");
        }


    }
}