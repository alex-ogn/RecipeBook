using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using RecipeBook.Constants;
using RecipeBook.Resources;
using RecipeBook.Models;
using RecipeBook.ViewModels.Account;
using RecipeBook.Services;

namespace RecipeBook.Controllers;

public class AccountController : Controller
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IEmailSender _emailSender;

    public AccountController(SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager, IEmailSender emailSender)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _emailSender = emailSender;
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
            ? string.Format(MessageTexts.AccountLockedOut, AccountSecurityConstants.LockoutMinutes)
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

            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);

            // Create URL link for email confirmation
            var confirmationLink = Url.Action(
                action: "ConfirmEmail",
                controller: "Account",
                values: new { userId = user.Id, token = token },
                protocol: Request.Scheme);

            await _emailSender.SendEmailAsync(
                        user.Email,
                        MessageTexts.ConfirmEmailSubject,
                        string.Format(MessageTexts.ConfirmEmailBody, confirmationLink));

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

        return View("Error");
    }
}