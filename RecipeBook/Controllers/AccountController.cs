using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using RecipeBook.Constants;
using RecipeBook.Extensions;
using RecipeBook.Resources;
using RecipeBook.Models;
using RecipeBook.ViewModels.Account;
using RecipeBook.Services;

namespace RecipeBook.Controllers;

public class AccountController : Controller
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IAccountEmailService _accountEmails;
    private readonly IUserProfileService _userProfileService;

    public AccountController(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        IAccountEmailService accountEmails,
        IUserProfileService userProfileService)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _accountEmails = accountEmails;
        _userProfileService = userProfileService;
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewData[ViewDataKeys.ReturnUrl] = returnUrl;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting(AccountSecurityConstants.AuthRateLimitPolicy)]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        ViewData[ViewDataKeys.ReturnUrl] = returnUrl;

        if (!ModelState.IsValid)
            return View(model);

        var user = await _userManager.FindByNameAsync(model.Identifier)
            ?? await _userManager.FindByEmailAsync(model.Identifier);

        // Same message for an unknown user and a wrong password, so the form doesn't reveal which accounts exist.
        if (user == null)
        {
            ModelState.AddModelError(string.Empty, MessageTexts.InvalidLogin);
            return View(model);
        }

        // Counts wrong passwords and locks the account after MaxFailedLoginAttempts (see Program.cs).
        var result = await _signInManager.CheckPasswordSignInAsync(user, model.Password, lockoutOnFailure: true);

        if (result.Succeeded)
        {
            await _signInManager.SignInAsync(user, model.RememberMe);
            return RedirectToLocal(returnUrl);
        }

        var error = result.IsLockedOut
            ? user.IsDisabledByAdmin()
                ? MessageTexts.AccountDisabled
                : string.Format(MessageTexts.AccountLockedOut, AccountSecurityConstants.LockoutMinutes)
            : result.IsNotAllowed
                ? MessageTexts.EmailNotConfirmed
                : MessageTexts.InvalidLogin;

        ModelState.AddModelError(string.Empty, error);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }

    // Where the authentication cookie sends a signed-in user who lacks the rights for a page
    // (CookieConstants.AccessDeniedPath).
    [HttpGet]
    public IActionResult AccessDenied()
    {
        return View();
    }

    private IActionResult RedirectToLocal(string? returnUrl)
    {
        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);

        return RedirectToAction("Index", "Home");
    }

    private async Task<string> GenerateUniqueUserNameAsync(string email)
    {
        var baseName = email.Split('@')[0];
        var username = baseName;
        int suffix = 0;

        while (await _userManager.FindByNameAsync(username) != null)
        {
            suffix++;
            username = baseName + suffix;
        }

        return username;
    }


    [HttpGet]
    public IActionResult Register(string? returnUrl = null)
    {
        ViewData[ViewDataKeys.ReturnUrl] = returnUrl;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting(AccountSecurityConstants.AuthRateLimitPolicy)]
    public async Task<IActionResult> Register(RegisterViewModel model, string? returnUrl = null)
    {
        if (!ModelState.IsValid)
            return View(model);

        // An address that is already registered gets the same answer as a new one,
        // so the form doesn't reveal which emails have accounts.
        if (await _userManager.FindByEmailAsync(model.Email) != null)
            return RedirectToAction(nameof(RegisterConfirmation));

        var generatedUserName = await GenerateUniqueUserNameAsync(model.Email);

        var user = new ApplicationUser
        {
            Email = model.Email,
            UserName = generatedUserName
        };

        var result = await _userManager.CreateAsync(user, model.Password);
        if (result.Succeeded)
        {
            await _userManager.AddToRoleAsync(user, UserRoles.User);
            await _accountEmails.SendEmailConfirmationAsync(user);

            return RedirectToAction(nameof(RegisterConfirmation));
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }

        return View(model);
    }

    [HttpGet]
    public IActionResult RegisterConfirmation()
    {
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> ConfirmEmail(string userId, string token)
    {
        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(token))
        {
            return RedirectToAction("Index", "Home");
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return NotFound(string.Format(MessageTexts.UserNotFoundById, userId));
        }

        var result = await _userManager.ConfirmEmailAsync(user, token);

        if (result.Succeeded)
        {
            return View("ConfirmEmailSuccess");
        }

        return View("InvalidLink");
    }

    [HttpGet]
    public IActionResult ResendConfirmation()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting(AccountSecurityConstants.AuthRateLimitPolicy)]
    public async Task<IActionResult> ResendConfirmation(ResendConfirmationViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user != null && !user.EmailConfirmed)
        {
            await _accountEmails.SendEmailConfirmationAsync(user);
        }

        // Same page whether or not an email was sent, so the form doesn't reveal which accounts exist.
        return RedirectToAction(nameof(RegisterConfirmation));
    }

    [HttpGet]
    public IActionResult ForgotPassword()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting(AccountSecurityConstants.AuthRateLimitPolicy)]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user != null && user.EmailConfirmed)
        {
            await _accountEmails.SendPasswordResetAsync(user);
        }

        // Same page whether or not an email was sent, so the form doesn't reveal which accounts exist.
        return RedirectToAction(nameof(ForgotPasswordConfirmation));
    }

    [HttpGet]
    public IActionResult ForgotPasswordConfirmation()
    {
        return View();
    }

    [HttpGet]
    public IActionResult ResetPassword(string? email, string? token)
    {
        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(token))
        {
            return RedirectToAction(nameof(ForgotPassword));
        }

        return View(new ResetPasswordViewModel { Email = email, Token = token });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting(AccountSecurityConstants.AuthRateLimitPolicy)]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user == null)
        {
            // Don't reveal that the account doesn't exist.
            return RedirectToAction(nameof(ResetPasswordConfirmation));
        }

        var result = await _userManager.ResetPasswordAsync(user, model.Token, model.Password);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        // The owner proved access to the mailbox: clear the wrong-password lockout, but not a lock set by an admin.
        if (!user.IsDisabledByAdmin())
        {
            await _userManager.SetLockoutEndDateAsync(user, null);
        }
        await _userManager.ResetAccessFailedCountAsync(user);

        return RedirectToAction(nameof(ResetPasswordConfirmation));
    }

    [HttpGet]
    public IActionResult ResetPasswordConfirmation()
    {
        return View();
    }

    [Authorize]
    [HttpGet]
    public IActionResult ChangePassword()
    {
        return View();
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting(AccountSecurityConstants.AuthRateLimitPolicy)]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        if (!await CheckCurrentPasswordAsync(user, model.CurrentPassword))
            return View(model);

        // Validates the new password and updates the security stamp, which signs out the user's other sessions.
        var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        await _signInManager.RefreshSignInAsync(user);
        await _accountEmails.SendPasswordChangedNoticeAsync(user);

        TempData[TempDataKeys.SuccessMessage] = MessageTexts.PasswordChanged;
        return RedirectToAction(nameof(UsersController.Edit), "Users");
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> ChangeEmail()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        return View(new ChangeEmailViewModel { CurrentEmail = user.Email ?? string.Empty });
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting(AccountSecurityConstants.AuthRateLimitPolicy)]
    public async Task<IActionResult> ChangeEmail(ChangeEmailViewModel model)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        model.CurrentEmail = user.Email ?? string.Empty;

        if (!ModelState.IsValid)
            return View(model);

        if (!await CheckCurrentPasswordAsync(user, model.CurrentPassword))
            return View(model);

        // The email changes only when the link sent to the new address is opened. An address that already
        // belongs to an account gets no email, but the same answer, so the form doesn't reveal which emails are taken.
        var sameEmail = string.Equals(user.Email, model.NewEmail, StringComparison.OrdinalIgnoreCase);
        if (!sameEmail && await _userManager.FindByEmailAsync(model.NewEmail) == null)
        {
            await _accountEmails.SendEmailChangeAsync(user, model.NewEmail);
        }

        return RedirectToAction(nameof(ChangeEmailSent));
    }

    [Authorize]
    [HttpGet]
    public IActionResult ChangeEmailSent()
    {
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> ConfirmEmailChange(string? userId, string? email, string? token)
    {
        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(email) || string.IsNullOrEmpty(token))
        {
            return RedirectToAction("Index", "Home");
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return View("InvalidLink");
        }

        // Checks the token, sets the email, marks it confirmed and updates the security stamp.
        var result = await _userManager.ChangeEmailAsync(user, email, token);
        if (!result.Succeeded)
        {
            return View("InvalidLink");
        }

        // Keep the browser that opened the link signed in, if it belongs to this user.
        if (User.FindFirstValue(ClaimTypes.NameIdentifier) == user.Id)
        {
            await _signInManager.RefreshSignInAsync(user);
        }

        return View("ConfirmEmailChangeSuccess");
    }

    [Authorize]
    [HttpGet]
    public IActionResult DeleteAccount()
    {
        return View();
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting(AccountSecurityConstants.AuthRateLimitPolicy)]
    public async Task<IActionResult> DeleteAccount(DeleteAccountViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        if (!await CheckCurrentPasswordAsync(user, model.CurrentPassword))
            return View(model);

        var result = await _userProfileService.DeleteOwnAccountAsync(user.Id);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        await _signInManager.SignOutAsync();

        TempData[TempDataKeys.SuccessMessage] = MessageTexts.AccountDeleted;
        return RedirectToAction("Index", "Home");
    }

    /// <summary>
    /// Checks the signed-in user's password before a sensitive change. Wrong passwords count towards the
    /// lockout, like on the login form, so these forms can't be used to guess the password.
    /// </summary>
    private async Task<bool> CheckCurrentPasswordAsync(ApplicationUser user, string password)
    {
        var result = await _signInManager.CheckPasswordSignInAsync(user, password, lockoutOnFailure: true);
        if (result.Succeeded)
            return true;

        ModelState.AddModelError(string.Empty, result.IsLockedOut
            ? string.Format(MessageTexts.AccountLockedOut, AccountSecurityConstants.LockoutMinutes)
            : MessageTexts.IncorrectPassword);

        return false;
    }
}
