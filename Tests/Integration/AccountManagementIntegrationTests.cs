using System.Net;
using RecipeBook.Constants;
using RecipeBook.Extensions;
using RecipeBook.Resources;

namespace Tests.Integration;

// Each class gets its own app instance (own database and rate-limit budget): the account forms are rate limited per
// client, and all test clients share one address.

/// <summary>
/// Changing the password needs the current password and renews the security stamp (signing out other sessions).
/// </summary>
public class ChangePasswordIntegrationTests : IClassFixture<RecipeBookFactory>
{
    private const string Url = "/Account/ChangePassword";
    private const string NewPassword = "New#Pass456";
    private readonly RecipeBookFactory _factory;

    public ChangePasswordIntegrationTests(RecipeBookFactory factory)
    {
        _factory = factory;
    }

    [Theory]
    [InlineData("/Account/ChangePassword")]
    [InlineData("/Account/ChangeEmail")]
    [InlineData("/Account/DeleteAccount")]
    public async Task Anonymous_IsRedirectedToLogin(string url)
    {
        var response = await _factory.CreateClientAs(null).GetAsync(url);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.StartsWith(LoginForm.Url, response.Headers.Location!.OriginalString);
    }

    [Fact]
    public async Task Post_WithoutAntiforgeryToken_IsRejected()
    {
        var user = await _factory.CreateLoginUserAsync("pw-noforgery", LoginForm.Password);
        var client = _factory.CreateClientAs(user.Id, UserRoles.User);

        var response = await client.PostFormAsync(Url, token: null,
            ("CurrentPassword", LoginForm.Password), ("NewPassword", NewPassword), ("ConfirmNewPassword", NewPassword));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.True(await _factory.HasPasswordAsync(user.Id, LoginForm.Password));
    }

    [Fact]
    public async Task WrongCurrentPassword_ChangesNothing()
    {
        var user = await _factory.CreateLoginUserAsync("pw-wrong", LoginForm.Password);
        var client = _factory.CreateClientAs(user.Id, UserRoles.User);
        var token = await client.GetAntiforgeryTokenAsync(Url);

        var response = await client.PostFormAsync(Url, token,
            ("CurrentPassword", "Wrong#Pass1"), ("NewPassword", NewPassword), ("ConfirmNewPassword", NewPassword));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains(MessageTexts.IncorrectPassword, await LoginForm.TextAsync(response));
        Assert.True(await _factory.HasPasswordAsync(user.Id, LoginForm.Password));
        Assert.False(await _factory.HasPasswordAsync(user.Id, NewPassword));
    }

    [Fact]
    public async Task CorrectCurrentPassword_ChangesPassword_AndNotifiesTheUser()
    {
        var user = await _factory.CreateLoginUserAsync("pw-right", LoginForm.Password);
        var stampBefore = user.SecurityStamp;
        var client = _factory.CreateClientAs(user.Id, UserRoles.User);
        var token = await client.GetAntiforgeryTokenAsync(Url);

        var response = await client.PostFormAsync(Url, token,
            ("CurrentPassword", LoginForm.Password), ("NewPassword", NewPassword), ("ConfirmNewPassword", NewPassword));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.True(await _factory.HasPasswordAsync(user.Id, NewPassword));
        Assert.False(await _factory.HasPasswordAsync(user.Id, LoginForm.Password));
        // A new security stamp signs out the user's other sessions.
        Assert.NotEqual(stampBefore, (await _factory.FindUserAsync(user.Id))!.SecurityStamp);
        Assert.Contains(_factory.Emails.SentTo(user.Email!), e => e.Subject == MessageTexts.PasswordChangedNoticeSubject);
    }
}

public class ForgotPasswordIntegrationTests : IClassFixture<RecipeBookFactory>
{
    private const string Url = "/Account/ForgotPassword";
    private const string NewPassword = "Reset#Pass789";
    private readonly RecipeBookFactory _factory;

    public ForgotPasswordIntegrationTests(RecipeBookFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task UnknownAndKnownEmail_GetTheSameAnswer_ButOnlyTheAccountGetsAnEmail()
    {
        var user = await _factory.CreateLoginUserAsync("forgot-same", LoginForm.Password);
        var client = _factory.CreateClientAs(null);
        var token = await client.GetAntiforgeryTokenAsync(Url);

        var unknown = await client.PostFormAsync(Url, token, ("Email", "nobody@test.local"));
        var known = await client.PostFormAsync(Url, token, ("Email", user.Email!));

        Assert.Equal(HttpStatusCode.Redirect, unknown.StatusCode);
        Assert.Equal(unknown.Headers.Location, known.Headers.Location);
        Assert.Empty(_factory.Emails.SentTo("nobody@test.local"));
        Assert.Single(_factory.Emails.SentTo(user.Email!), e => e.Subject == MessageTexts.PasswordResetEmailSubject);
    }

    [Fact]
    public async Task EmailedLink_SetsTheNewPassword()
    {
        var user = await _factory.CreateLoginUserAsync("forgot-link", LoginForm.Password);
        var client = _factory.CreateClientAs(null);
        await client.PostFormAsync(Url, await client.GetAntiforgeryTokenAsync(Url), ("Email", user.Email!));

        var link = _factory.Emails.SentTo(user.Email!).Single(e => e.Subject == MessageTexts.PasswordResetEmailSubject).LinkPathAndQuery;
        var resetToken = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(new Uri(new Uri("http://localhost"), link).Query)["token"].ToString();

        var response = await client.PostFormAsync("/Account/ResetPassword", await client.GetAntiforgeryTokenAsync(link),
            ("Email", user.Email!), ("Token", resetToken), ("Password", NewPassword), ("ConfirmPassword", NewPassword));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/Account/ResetPasswordConfirmation", response.Headers.Location!.OriginalString);
        Assert.True(await _factory.HasPasswordAsync(user.Id, NewPassword));
    }

    [Fact]
    public async Task InvalidToken_ChangesNothing()
    {
        var user = await _factory.CreateLoginUserAsync("forgot-badtoken", LoginForm.Password);
        var client = _factory.CreateClientAs(null);
        var page = $"/Account/ResetPassword?email={Uri.EscapeDataString(user.Email!)}&token=forged";

        var response = await client.PostFormAsync("/Account/ResetPassword", await client.GetAntiforgeryTokenAsync(page),
            ("Email", user.Email!), ("Token", "forged"), ("Password", NewPassword), ("ConfirmPassword", NewPassword));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(await _factory.HasPasswordAsync(user.Id, LoginForm.Password));
    }
}

public class ChangeEmailIntegrationTests : IClassFixture<RecipeBookFactory>
{
    private const string Url = "/Account/ChangeEmail";
    private readonly RecipeBookFactory _factory;

    public ChangeEmailIntegrationTests(RecipeBookFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task WrongPassword_SendsNoEmail()
    {
        var user = await _factory.CreateLoginUserAsync("email-wrongpw", LoginForm.Password);
        var client = _factory.CreateClientAs(user.Id, UserRoles.User);

        var response = await client.PostFormAsync(Url, await client.GetAntiforgeryTokenAsync(Url),
            ("NewEmail", "email-wrongpw-new@test.local"), ("CurrentPassword", "Wrong#Pass1"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains(MessageTexts.IncorrectPassword, await LoginForm.TextAsync(response));
        Assert.Empty(_factory.Emails.SentTo("email-wrongpw-new@test.local"));
    }

    [Fact]
    public async Task EmailChanges_OnlyAfterTheLinkIsOpened_AndStaysConfirmed()
    {
        const string newEmail = "email-ok-new@test.local";
        var user = await _factory.CreateLoginUserAsync("email-ok", LoginForm.Password);
        var client = _factory.CreateClientAs(user.Id, UserRoles.User);

        var response = await client.PostFormAsync(Url, await client.GetAntiforgeryTokenAsync(Url),
            ("NewEmail", newEmail), ("CurrentPassword", LoginForm.Password));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal(user.Email, (await _factory.FindUserAsync(user.Id))!.Email);
        Assert.Contains(_factory.Emails.SentTo(user.Email!), e => e.Subject == MessageTexts.EmailChangeNoticeSubject);

        var link = _factory.Emails.SentTo(newEmail).Single(e => e.Subject == MessageTexts.ChangeEmailSubject).LinkPathAndQuery;
        var confirm = await client.GetAsync(link);

        Assert.Equal(HttpStatusCode.OK, confirm.StatusCode);
        var updated = (await _factory.FindUserAsync(user.Id))!;
        Assert.Equal(newEmail, updated.Email);
        Assert.True(updated.EmailConfirmed);
    }

    [Fact]
    public async Task AddressOfAnotherAccount_IsNotTakenOver()
    {
        var other = await _factory.CreateLoginUserAsync("email-taken-owner", LoginForm.Password);
        var user = await _factory.CreateLoginUserAsync("email-taken-thief", LoginForm.Password);
        var client = _factory.CreateClientAs(user.Id, UserRoles.User);

        var response = await client.PostFormAsync(Url, await client.GetAntiforgeryTokenAsync(Url),
            ("NewEmail", other.Email!), ("CurrentPassword", LoginForm.Password));

        // Same answer as for a free address, but no link is sent and nothing changes.
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/Account/ChangeEmailSent", response.Headers.Location!.OriginalString);
        Assert.DoesNotContain(_factory.Emails.SentTo(other.Email!), e => e.Subject == MessageTexts.ChangeEmailSubject);
        Assert.Equal(user.Email, (await _factory.FindUserAsync(user.Id))!.Email);
    }

    [Fact]
    public async Task ForgedLink_ChangesNothing()
    {
        var user = await _factory.CreateLoginUserAsync("email-forged", LoginForm.Password);

        var response = await _factory.CreateClientAs(null)
            .GetAsync($"/Account/ConfirmEmailChange?userId={user.Id}&email=attacker%40test.local&token=forged");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(user.Email, (await _factory.FindUserAsync(user.Id))!.Email);
    }
}

public class ConfirmationEmailIntegrationTests : IClassFixture<RecipeBookFactory>
{
    private const string Url = "/Account/ResendConfirmation";
    private readonly RecipeBookFactory _factory;

    public ConfirmationEmailIntegrationTests(RecipeBookFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Resend_SendsOnlyToUnconfirmedAccounts_WithTheSameAnswerForAll()
    {
        var unconfirmed = await _factory.CreateLoginUserAsync("resend-unconfirmed", LoginForm.Password, emailConfirmed: false);
        var confirmed = await _factory.CreateLoginUserAsync("resend-confirmed", LoginForm.Password);
        var client = _factory.CreateClientAs(null);
        var token = await client.GetAntiforgeryTokenAsync(Url);

        var responses = new[]
        {
            await client.PostFormAsync(Url, token, ("Email", unconfirmed.Email!)),
            await client.PostFormAsync(Url, token, ("Email", confirmed.Email!)),
            await client.PostFormAsync(Url, token, ("Email", "resend-nobody@test.local")),
        };

        Assert.All(responses, r => Assert.Equal("/Account/RegisterConfirmation", r.Headers.Location!.OriginalString));
        Assert.Single(_factory.Emails.SentTo(unconfirmed.Email!), e => e.Subject == MessageTexts.ConfirmEmailSubject);
        Assert.Empty(_factory.Emails.SentTo(confirmed.Email!));
        Assert.Empty(_factory.Emails.SentTo("resend-nobody@test.local"));
    }

    [Fact]
    public async Task Register_WithATakenEmail_LooksLikeSuccess_ButCreatesNothing()
    {
        var existing = await _factory.CreateLoginUserAsync("register-taken", LoginForm.Password);
        var client = _factory.CreateClientAs(null);

        var response = await client.PostFormAsync("/Account/Register", await client.GetAntiforgeryTokenAsync("/Account/Register"),
            ("Email", existing.Email!), ("Password", "Other#Pass1"), ("ConfirmPassword", "Other#Pass1"));

        Assert.Equal("/Account/RegisterConfirmation", response.Headers.Location!.OriginalString);
        Assert.Empty(_factory.Emails.SentTo(existing.Email!));
        Assert.True(await _factory.HasPasswordAsync(existing.Id, LoginForm.Password));
    }
}

public class DeleteAccountIntegrationTests : IClassFixture<RecipeBookFactory>
{
    private const string Url = "/Account/DeleteAccount";
    private readonly RecipeBookFactory _factory;

    public DeleteAccountIntegrationTests(RecipeBookFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task WrongPassword_KeepsTheAccount()
    {
        var user = await _factory.CreateLoginUserAsync("delete-wrongpw", LoginForm.Password);
        var client = _factory.CreateClientAs(user.Id, UserRoles.User);

        var response = await client.PostFormAsync(Url, await client.GetAntiforgeryTokenAsync(Url),
            ("CurrentPassword", "Wrong#Pass1"), ("Confirm", "true"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(await _factory.FindUserAsync(user.Id));
    }

    [Fact]
    public async Task CorrectPassword_DeletesTheAccount()
    {
        var user = await _factory.CreateLoginUserAsync("delete-ok", LoginForm.Password);
        var client = _factory.CreateClientAs(user.Id, UserRoles.User);

        var response = await client.PostFormAsync(Url, await client.GetAntiforgeryTokenAsync(Url),
            ("CurrentPassword", LoginForm.Password), ("Confirm", "true"));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/", response.Headers.Location!.OriginalString);
        Assert.Null(await _factory.FindUserAsync(user.Id));
    }

    [Fact]
    public async Task Admin_CannotDeleteOwnAccount()
    {
        var admin = await _factory.CreateLoginUserAsync("delete-admin", LoginForm.Password);
        await _factory.AddToRoleAsync(admin.UserName!, UserRoles.Admin);
        var client = _factory.CreateClientAs(admin.Id, UserRoles.Admin);

        var response = await client.PostFormAsync(Url, await client.GetAntiforgeryTokenAsync(Url),
            ("CurrentPassword", LoginForm.Password), ("Confirm", "true"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains(MessageTexts.AdminCannotDeleteOwnAccount, await LoginForm.TextAsync(response));
        Assert.NotNull(await _factory.FindUserAsync(admin.Id));
    }
}

/// <summary>
/// Admin-only account actions: send a reset link, disable and re-enable an account; and email privacy in the user list.
/// </summary>
public class AdminAccountIntegrationTests : IClassFixture<RecipeBookFactory>, IAsyncLifetime
{
    private readonly RecipeBookFactory _factory;

    public AdminAccountIntegrationTests(RecipeBookFactory factory)
    {
        _factory = factory;
    }

    public Task InitializeAsync() => _factory.SeedAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Theory]
    [InlineData("SendPasswordReset")]
    [InlineData("Lock")]
    [InlineData("Unlock")]
    public async Task AdminActions_RedirectAnonymous_AndForbidUsers(string action)
    {
        var url = $"/Users/{action}/{RecipeBookFactory.AuthorId}";

        var anonymous = _factory.CreateClientAs(null);
        var asAnonymous = await anonymous.PostFormAsync(url, await anonymous.GetAntiforgeryTokenAsync(LoginForm.Url));

        var user = _factory.CreateClientAs(RecipeBookFactory.StrangerId, UserRoles.User);
        var asUser = await user.PostFormAsync(url, await user.GetAntiforgeryTokenAsync($"/Users/Edit/{RecipeBookFactory.StrangerId}"));

        Assert.Equal(HttpStatusCode.Redirect, asAnonymous.StatusCode);
        Assert.StartsWith(LoginForm.Url, asAnonymous.Headers.Location!.OriginalString);
        Assert.Equal(HttpStatusCode.Forbidden, asUser.StatusCode);
        Assert.Null((await _factory.FindUserAsync(RecipeBookFactory.AuthorId))!.LockoutEnd);
    }

    [Fact]
    public async Task Admin_SendsResetLink()
    {
        var user = await _factory.CreateLoginUserAsync("admin-reset-target", LoginForm.Password);
        var admin = AdminClient();

        var response = await admin.PostFormAsync($"/Users/SendPasswordReset/{user.Id}", await admin.GetAntiforgeryTokenAsync($"/Users/Edit/{user.Id}"));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Single(_factory.Emails.SentTo(user.Email!), e => e.Subject == MessageTexts.PasswordResetEmailSubject);
        // The admin never sets the password itself.
        Assert.True(await _factory.HasPasswordAsync(user.Id, LoginForm.Password));
    }

    [Fact]
    public async Task Admin_LocksAccount_ThenLoginIsRefused_AndUnlockRestoresIt()
    {
        var user = await _factory.CreateLoginUserAsync("admin-lock-target", LoginForm.Password);
        var stampBefore = user.SecurityStamp;
        var admin = AdminClient();
        var token = await admin.GetAntiforgeryTokenAsync($"/Users/Edit/{user.Id}");

        await admin.PostFormAsync($"/Users/Lock/{user.Id}", token);

        var locked = (await _factory.FindUserAsync(user.Id))!;
        Assert.True(locked.IsDisabledByAdmin());
        Assert.NotEqual(stampBefore, locked.SecurityStamp);

        var login = await LoginForm.PostAsync(_factory.CreateClientAs(null), user.UserName!, LoginForm.Password);
        Assert.Contains(MessageTexts.AccountDisabled, await LoginForm.TextAsync(login));

        await admin.PostFormAsync($"/Users/Unlock/{user.Id}", token);

        Assert.False((await _factory.FindUserAsync(user.Id))!.IsLockedOutNow());
    }

    [Fact]
    public async Task Admin_CannotLockThemself()
    {
        var admin = AdminClient();

        var response = await admin.PostFormAsync($"/Users/Lock/{RecipeBookFactory.AdminId}",
            await admin.GetAntiforgeryTokenAsync($"/Users/Edit/{RecipeBookFactory.AdminId}"));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.False((await _factory.FindUserAsync(RecipeBookFactory.AdminId))!.IsLockedOutNow());
    }

    [Fact]
    public async Task UserList_ShowsAndSearchesEmails_OnlyForAdmins()
    {
        var asUser = await (await _factory.CreateClientAs(RecipeBookFactory.StrangerId, UserRoles.User).GetAsync("/Users")).Content.ReadAsStringAsync();
        var asAdmin = await (await AdminClient().GetAsync("/Users")).Content.ReadAsStringAsync();
        var userSearchByEmail = await (await _factory.CreateClientAs(RecipeBookFactory.StrangerId, UserRoles.User).GetAsync("/Users?search=author%40test")).Content.ReadAsStringAsync();

        Assert.DoesNotContain("author@test.local", asUser);
        Assert.Contains("author@test.local", asAdmin);
        Assert.DoesNotContain($"userId={RecipeBookFactory.AuthorId}&", userSearchByEmail);
    }

    private HttpClient AdminClient() => _factory.CreateClientAs(RecipeBookFactory.AdminId, UserRoles.Admin);
}
