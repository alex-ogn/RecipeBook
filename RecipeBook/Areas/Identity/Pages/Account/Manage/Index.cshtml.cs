// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RecipeBook.Models;
using RecipeBook.Services;
using RecipeBook.ViewModels.Users;

namespace RecipeBook.Areas.Identity.Pages.Account.Manage
{
    public class IndexModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IUserProfileService _userProfileService;

        public IndexModel(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IUserProfileService userProfileService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _userProfileService = userProfileService;
        }
        [BindProperty]
        public InputModel Input { get; set; }

        [TempData]
        public string SuccessMessage { get; set; }

        /// </summary>
        public class InputModel
        {
            [Display(Name = "Потребителско име")]
            public string? UserName { get; set; }

            [Phone]
            [Display(Name = "Телефон")]
            public string PhoneNumber { get; set; }

            [Display(Name = "Профилна снимка")]
            public IFormFile? ProfilePicture { get; set; }

            public string Id { get; set; }

            public int ProfilePictureVersion { get; set; }
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            Input = new InputModel
            {
                UserName = user.UserName,
                PhoneNumber = user.PhoneNumber,
                Id = user.Id,
                ProfilePictureVersion = user.ProfilePictureVersion
            };

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            if (!ModelState.IsValid)
            {
                return Page();
            }

            // Използване на общия сервиз
            var result = await _userProfileService.UpdateUserProfileAsync(user, new ViewModels.Users.EditUserViewModel
            {
                Id = user.Id,
                UserName = Input.UserName,
                PhoneNumber = Input.PhoneNumber,
                ProfilePicture = Input.ProfilePicture
            }, isAdmin: false);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                    ModelState.AddModelError(string.Empty, error.Description);

                return Page();
            }

            await _signInManager.RefreshSignInAsync(user);
            TempData["SuccessMessage"] = "Профилът беше успешно обновен.";
            return RedirectToPage();
        }


    }
}
