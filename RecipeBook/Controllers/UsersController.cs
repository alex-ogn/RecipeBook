using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RecipeBook.Extensions;
using RecipeBook.Models;
using RecipeBook.Services;
using RecipeBook.ViewModels.Users;
using System.Security.Claims;
using RecipeBook.Constants;
using RecipeBook.Resources;

namespace RecipeBook.Controllers
{

    public class UsersController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IUserProfileService _userProfileService;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IImageStorageService _imageStorage;
        private readonly IFollowService _followService;

        public UsersController(
            UserManager<ApplicationUser> userManager,
            IUserProfileService userProfileService,
            SignInManager<ApplicationUser> signInManager,
            IImageStorageService imageStorage,
            IFollowService followService)
        {
            _userManager = userManager;
            _userProfileService = userProfileService;
            _signInManager = signInManager;
            _imageStorage = imageStorage;
            _followService = followService;
        }

        [Authorize]
        // Показва списък с всички потребители
        public async Task<IActionResult> Index(string search, int page = 1)
        {
            var usersQuery = _userManager.Users.AsNoTracking();

            if (!string.IsNullOrEmpty(search))
            {
                usersQuery = usersQuery.Where(u =>
                    u.UserName!.Contains(search) || u.Email!.Contains(search));
            }

            var users = await usersQuery
                .OrderBy(u => u.UserName)
                .ToPagedResultAsync(page, PagingConstants.UsersPageSize);

            ViewData[ViewDataKeys.Search] = search;
            return View(users);
        }

        [Authorize(Roles = UserRoles.Admin)]
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
            var isAdmin = User.IsInRole(UserRoles.Admin);

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

            ViewData[ViewDataKeys.IsUserAdmin] = isAdmin;

            return View(model);
        }


        // POST: Users/Edit/id
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditUserViewModel model)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole(UserRoles.Admin);

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
                TempData[TempDataKeys.ErrorMessage] = string.Join("; ", result.Errors.Select(e => e.Description));
                return View(model);
            }

            if (!isAdmin)
                await _signInManager.RefreshSignInAsync(user); // важи само за текущия юзър

            TempData[TempDataKeys.SuccessMessage] = MessageTexts.ProfileUpdated;
            return isAdmin ? RedirectToAction(nameof(Index)) : RedirectToAction(nameof(Edit));
        }



        [Authorize]
        [HttpPost]
        public async Task<IActionResult> UploadProfilePicture(IFormFile profilePicture)
        {
            var user = await _userManager.GetUserAsync(User);

            if (profilePicture != null && profilePicture.Length > 0)
            {
                var imageError = _imageStorage.Validate(profilePicture);
                if (imageError != null)
                {
                    TempData[TempDataKeys.ErrorMessage] = imageError;
                    return RedirectToAction(nameof(Index));
                }

                using var memoryStream = new MemoryStream();
                await profilePicture.CopyToAsync(memoryStream);
                user.ProfilePicture = memoryStream.ToArray();

                await _userManager.UpdateAsync(user);
            }

            return RedirectToAction(nameof(Index));
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
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

            if (!await _followService.FollowAsync(currentUserId, userId))
            {
                return BadRequest();
            }

            // върни се към страницата, от която е подадена формата
            return this.RedirectToLocalReferer(ProfileUrl(userId));
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Unfollow(string userId)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

            await _followService.UnfollowAsync(currentUserId, userId);

            // върни се към страницата, от която е подадена формата
            return this.RedirectToLocalReferer(ProfileUrl(userId));
        }

        [HttpGet]
        public async Task<IActionResult> Followers(string id)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            ViewData[ViewDataKeys.ListTitle] = MessageTexts.FollowersTitle;
            ViewData[ViewDataKeys.CurrentUserId] = currentUserId;

            return View("FollowList", await _followService.GetFollowersAsync(id, currentUserId));
        }

        [HttpGet]
        public async Task<IActionResult> Following(string id)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            ViewData[ViewDataKeys.ListTitle] = MessageTexts.FollowingTitle;
            ViewData[ViewDataKeys.CurrentUserId] = currentUserId;

            return View("FollowList", await _followService.GetFollowingAsync(id, currentUserId));
        }

        private string ProfileUrl(string userId)
            => Url.Action(nameof(RecipesController.MyIndex), "Recipes", new { id = userId })!;

        [Authorize(Roles = UserRoles.Admin)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var result = await _userProfileService.DeleteUserAsync(id, currentUserId);

            if (!result.Succeeded)
            {
                string errorMsg = MessageTexts.UserNotDeleted;

                foreach (var error in result.Errors)
                {
                    errorMsg += " " + error.Description;
                }

                TempData[TempDataKeys.ErrorMessage] = errorMsg;
                return RedirectToAction(nameof(Edit), new { id });
            }

            TempData[TempDataKeys.SuccessMessage] = MessageTexts.UserDeleted;
            return RedirectToAction(nameof(Index));
        }


    }
}