using RecipeBook.Constants;
using RecipeBook.Models;

namespace RecipeBook.Extensions;

public static class ApplicationUserExtensions
{
    /// <summary>
    /// True while the account is locked out, either after too many wrong passwords or by an admin.
    /// </summary>
    public static bool IsLockedOutNow(this ApplicationUser user)
        => user.LockoutEnd > DateTimeOffset.UtcNow;

    /// <summary>
    /// True when an admin disabled the account. A lockout after wrong passwords never lasts longer than
    /// <see cref="AccountSecurityConstants.LockoutDuration"/>, so anything longer was set by an admin.
    /// </summary>
    public static bool IsDisabledByAdmin(this ApplicationUser user)
        => user.LockoutEnd > DateTimeOffset.UtcNow + AccountSecurityConstants.LockoutDuration;
}
