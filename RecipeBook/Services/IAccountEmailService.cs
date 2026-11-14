using RecipeBook.Models;

namespace RecipeBook.Services;

/// <summary>
/// Sends the account emails (confirmation, password reset, email change and security notices)
/// with links that carry an Identity token.
/// </summary>
public interface IAccountEmailService
{
    /// <summary>
    /// Sends a link that confirms the user's current email address.
    /// </summary>
    Task SendEmailConfirmationAsync(ApplicationUser user);

    /// <summary>
    /// Sends a link to the reset-password form to the user's email address.
    /// </summary>
    Task SendPasswordResetAsync(ApplicationUser user);

    /// <summary>
    /// Sends a confirmation link to <paramref name="newEmail"/> and a notice to the current address.
    /// The email changes only when the link is opened.
    /// </summary>
    Task SendEmailChangeAsync(ApplicationUser user, string newEmail);

    /// <summary>
    /// Tells the user that their password was changed.
    /// </summary>
    Task SendPasswordChangedNoticeAsync(ApplicationUser user);
}
