using Microsoft.AspNetCore.Identity;
using RecipeBook.Controllers;
using RecipeBook.Models;
using RecipeBook.Resources;

namespace RecipeBook.Services;

public class AccountEmailService : IAccountEmailService
{
    private const string AccountControllerName = "Account";

    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IEmailSender _emailSender;
    private readonly LinkGenerator _linkGenerator;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AccountEmailService(
        UserManager<ApplicationUser> userManager,
        IEmailSender emailSender,
        LinkGenerator linkGenerator,
        IHttpContextAccessor httpContextAccessor)
    {
        _userManager = userManager;
        _emailSender = emailSender;
        _linkGenerator = linkGenerator;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task SendEmailConfirmationAsync(ApplicationUser user)
    {
        var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
        var link = BuildLink(nameof(AccountController.ConfirmEmail), new { userId = user.Id, token });

        await _emailSender.SendEmailAsync(
            user.Email!,
            MessageTexts.ConfirmEmailSubject,
            string.Format(MessageTexts.ConfirmEmailBody, link));
    }

    public async Task SendPasswordResetAsync(ApplicationUser user)
    {
        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var link = BuildLink(nameof(AccountController.ResetPassword), new { email = user.Email, token });

        await _emailSender.SendEmailAsync(
            user.Email!,
            MessageTexts.PasswordResetEmailSubject,
            string.Format(MessageTexts.PasswordResetEmailBody, link));
    }

    public async Task SendEmailChangeAsync(ApplicationUser user, string newEmail)
    {
        var token = await _userManager.GenerateChangeEmailTokenAsync(user, newEmail);
        var link = BuildLink(nameof(AccountController.ConfirmEmailChange), new { userId = user.Id, email = newEmail, token });

        await _emailSender.SendEmailAsync(
            newEmail,
            MessageTexts.ChangeEmailSubject,
            string.Format(MessageTexts.ChangeEmailBody, link));

        if (!string.IsNullOrEmpty(user.Email))
        {
            await _emailSender.SendEmailAsync(
                user.Email,
                MessageTexts.EmailChangeNoticeSubject,
                string.Format(MessageTexts.EmailChangeNoticeBody, newEmail));
        }
    }

    public Task SendPasswordChangedNoticeAsync(ApplicationUser user)
        => _emailSender.SendEmailAsync(
            user.Email!,
            MessageTexts.PasswordChangedNoticeSubject,
            MessageTexts.PasswordChangedNoticeBody);

    private string BuildLink(string action, object values)
    {
        var httpContext = _httpContextAccessor.HttpContext
            ?? throw new InvalidOperationException("Account email links can only be built during an HTTP request.");

        return _linkGenerator.GetUriByAction(httpContext, action, AccountControllerName, values)
            ?? throw new InvalidOperationException($"No route found for Account/{action}.");
    }
}
