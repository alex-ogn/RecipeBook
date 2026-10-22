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

    // Rate limit for the Login and Register POSTs, per client IP address.
    public const string AuthRateLimitPolicy = "auth";
    public const int AuthRequestsPerWindow = 10;
    public static readonly TimeSpan AuthRateLimitWindow = TimeSpan.FromMinutes(1);
}
