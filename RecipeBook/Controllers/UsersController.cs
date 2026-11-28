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
        private readonly IFollowService _followService;
        private readonly IAccountEmailService _accountEmails;

        public UsersController(
            UserManager<ApplicationUser> userManager,
            IUserProfileService userProfileService,
            SignInManager<ApplicationUser> signInManager,
            IFollowService followService,
            IAccountEmailService accountEmails)
        {
            _userManager = userManager;
            _userProfileService = userProfileService;
            _signInManager = signInManager;
            _followService = followService;
            _accountEmails = accountEmails;
        }

        [Authorize]
        // Показва списък с всички потребители
        public async Task<IActionResult> Index(string search, int page = 1)
        {
            var usersQuery = _userManager.Users.AsNoTracking();

            if (!string.IsNullOrEmpty(search))
            {
                // Only admins see emails, so only admins can search by them.
                usersQuery = User.IsInRole(UserRoles.Admin)
                    ? usersQuery.Where(u => u.UserName!.Contains(search) || u.Email!.Contains(search))
                    : usersQuery.Where(u => u.UserName!.Contains(search));
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
            if (id == null) return NotFound();

            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            return View(ToEditModel(user, User.FindFirstValue(ClaimTypes.NameIdentifier)));
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

            var targetId = id ?? currentUserId!;
            var user = await _userManager.FindByIdAsync(targetId);
            if (user == null) return NotFound();

            var model = ToEditModel(user, currentUserId);

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

            var user = await _userManager.FindByIdAsync(model.Id);
            if (user == null) return NotFound();

            ViewData[ViewDataKeys.IsUserAdmin] = isAdmin;

            if (!ModelState.IsValid) return View(WithAccountState(model, user, currentUserId));

            var result = await _userProfileService.UpdateUserProfileAsync(user, model, isAdmin);
            if (!result.Succeeded)
            {
                TempData[TempDataKeys.ErrorMessage] = string.Join("; ", result.Errors.Select(e => e.Description));
                return View(WithAccountState(model, user, currentUserId));
            }

            if (user.Id == currentUserId)
                await _signInManager.RefreshSignInAsync(user); // важи само за текущия юзър

            TempData[TempDataKeys.SuccessMessage] = MessageTexts.ProfileUpdated;
            return isAdmin ? RedirectToAction(nameof(Index)) : RedirectToAction(nameof(Edit));
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

        private static EditUserViewModel ToEditModel(ApplicationUser user, string? currentUserId) => WithAccountState(new EditUserViewModel
        {
            Id = user.Id,
            UserName = user.UserName ?? string.Empty,
            PhoneNumber = user.PhoneNumber,
        }, user, currentUserId);

        /// <summary>
        /// Fills the read-only fields of the form (email and account state) from the database, never from the POST.
        /// </summary>
        private static EditUserViewModel WithAccountState(EditUserViewModel model, ApplicationUser user, string? currentUserId)
        {
            model.Email = user.Email ?? string.Empty;
            model.ExistingProfilePictureUrl = ImageConstants.ProfilePictureOrDefault(user.ProfilePictureUrl);
            model.EmailConfirmed = user.EmailConfirmed;
            model.IsLockedOut = user.IsLockedOutNow();
            model.IsDisabled = user.IsDisabledByAdmin();
            model.LockoutEnd = user.LockoutEnd;
            model.IsOwnProfile = user.Id == currentUserId;
            return model;
        }

        private string ProfileUrl(string userId)
            => Url.Action(nameof(RecipesController.MyIndex), "Recipes", new { id = userId })!;

        [Authorize(Roles = UserRoles.Admin)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
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

        // Admins never pick a password for a user: they send the user a link to set one.
        [Authorize(Roles = UserRoles.Admin)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendPasswordReset(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null || string.IsNullOrEmpty(user.Email)) return NotFound();

            await _accountEmails.SendPasswordResetAsync(user);

            TempData[TempDataKeys.SuccessMessage] = string.Format(MessageTexts.PasswordResetLinkSent, user.Email);
            return RedirectToAction(nameof(Edit), new { id });
        }

        [Authorize(Roles = UserRoles.Admin)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Lock(string id)
        {
            if (id == User.FindFirstValue(ClaimTypes.NameIdentifier))
            {
                TempData[TempDataKeys.ErrorMessage] = MessageTexts.CannotLockYourself;
                return RedirectToAction(nameof(Edit), new { id });
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            // Users created with lockout switched off (e.g. seeded ones) can't be locked out until it is switched on.
            await _userManager.SetLockoutEnabledAsync(user, true);
            await _userManager.SetLockoutEndDateAsync(user, AccountSecurityConstants.DisabledLockoutEnd);
            // Signs the user out of their current sessions.
            await _userManager.UpdateSecurityStampAsync(user);

            TempData[TempDataKeys.SuccessMessage] = string.Format(MessageTexts.UserLocked, user.UserName);
            return RedirectToAction(nameof(Edit), new { id });
        }

        [Authorize(Roles = UserRoles.Admin)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Unlock(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            await _userManager.SetLockoutEndDateAsync(user, null);
            await _userManager.ResetAccessFailedCountAsync(user);

            TempData[TempDataKeys.SuccessMessage] = string.Format(MessageTexts.UserUnlocked, user.UserName);
            return RedirectToAction(nameof(Edit), new { id });
        }


    }
}