namespace RecipeBook.Constants;

/// <summary>
/// Limits that protect the login and registration forms from password guessing and abuse.
/// </summary>
public static class AccountSecurityConstants
{
    // Identity lockout: after this many wrong passwords the account is locked for LockoutDuration.
    public const int MaxFailedLoginAttempts = 5;
    public const int LockoutMinutes = 15;
    public static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(LockoutMinutes);

    // An account disabled by an admin is locked out until this date (i.e. until an admin unlocks it).
    public static readonly DateTimeOffset DisabledLockoutEnd = DateTimeOffset.MaxValue;

    // How often a signed-in session is checked against the user's security stamp. After a password or email change,
    // a lock or a deletion, other sessions of that user are signed out within this time.
    public static readonly TimeSpan SecurityStampValidationInterval = TimeSpan.FromMinutes(1);

    // Rate limit for the login, registration, password and email form POSTs, per client IP address.
    public const string AuthRateLimitPolicy = "auth";
    public const int AuthRequestsPerWindow = 10;
    public static readonly TimeSpan AuthRateLimitWindow = TimeSpan.FromMinutes(1);
}
