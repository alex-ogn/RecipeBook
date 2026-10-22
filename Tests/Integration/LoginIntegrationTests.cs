using System.Net;
using RecipeBook.Constants;
using RecipeBook.Resources;

namespace Tests.Integration;

internal static class LoginForm
{
    public const string Url = "/Account/Login";
    public const string Password = "Test#Pass123";

    public static async Task<HttpResponseMessage> PostAsync(HttpClient client, string identifier, string password)
    {
        var token = await client.GetAntiforgeryTokenAsync(Url);
        return await client.PostFormAsync(Url, token, ("Identifier", identifier), ("Password", password));
    }

    /// <summary>
    /// The page text with HTML entities decoded (Razor encodes Cyrillic as entities).
    /// </summary>
    public static async Task<string> TextAsync(HttpResponseMessage response)
        => WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());
}

/// <summary>
/// Login form behavior through the real pipeline (Identity, antiforgery, rate limiter).
/// </summary>
public class LoginIntegrationTests : IClassFixture<RecipeBookFactory>
{
    private readonly RecipeBookFactory _factory;

    public LoginIntegrationTests(RecipeBookFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CorrectPassword_SignsInAndRedirects()
    {
        await _factory.CreateLoginUserAsync("goodlogin", LoginForm.Password);

        var response = await LoginForm.PostAsync(_factory.CreateClientAs(null), "goodlogin", LoginForm.Password);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/", response.Headers.Location!.OriginalString);
    }

    [Fact]
    public async Task UnknownUserAndWrongPassword_ShowTheSameMessage()
    {
        await _factory.CreateLoginUserAsync("known", LoginForm.Password);
        var client = _factory.CreateClientAs(null);

        var unknownUser = await LoginForm.TextAsync(await LoginForm.PostAsync(client, "nobody", LoginForm.Password));
        var wrongPassword = await LoginForm.TextAsync(await LoginForm.PostAsync(client, "known", "Wrong#Pass1"));

        Assert.Contains(MessageTexts.InvalidLogin, unknownUser);
        Assert.Contains(MessageTexts.InvalidLogin, wrongPassword);
    }

    [Fact]
    public async Task UnconfirmedEmail_AsksToConfirm()
    {
        await _factory.CreateLoginUserAsync("unconfirmed", LoginForm.Password, emailConfirmed: false);

        var response = await LoginForm.PostAsync(_factory.CreateClientAs(null), "unconfirmed", LoginForm.Password);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains(MessageTexts.EmailNotConfirmed, await LoginForm.TextAsync(response));
    }
}

/// <summary>
/// Kept in its own class (own app instance) so its many requests don't use up the rate limit of other tests.
/// </summary>
public class LoginLockoutIntegrationTests : IClassFixture<RecipeBookFactory>
{
    private readonly RecipeBookFactory _factory;

    public LoginLockoutIntegrationTests(RecipeBookFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task RepeatedWrongPasswords_LockTheAccount_EvenForTheRightPassword()
    {
        await _factory.CreateLoginUserAsync("victim", LoginForm.Password);
        var client = _factory.CreateClientAs(null);

        for (var attempt = 0; attempt < AccountSecurityConstants.MaxFailedLoginAttempts; attempt++)
        {
            await LoginForm.PostAsync(client, "victim", "Wrong#Pass1");
        }

        var response = await LoginForm.PostAsync(client, "victim", LoginForm.Password);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains(string.Format(MessageTexts.AccountLockedOut, AccountSecurityConstants.LockoutMinutes), await LoginForm.TextAsync(response));
        Assert.True(await _factory.IsLockedOutAsync("victim"));
    }
}

/// <summary>
/// Own app instance: this test deliberately exhausts the rate limit.
/// </summary>
public class AuthRateLimitIntegrationTests : IClassFixture<RecipeBookFactory>
{
    private readonly RecipeBookFactory _factory;

    public AuthRateLimitIntegrationTests(RecipeBookFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task TooManyLoginPosts_AreRejectedWith429()
    {
        var client = _factory.CreateClientAs(null);

        // Requests inside the limit reach the app (here: rejected by antiforgery, as they carry no token).
        for (var i = 0; i < AccountSecurityConstants.AuthRequestsPerWindow; i++)
        {
            var allowed = await client.PostFormAsync(LoginForm.Url, token: null);
            Assert.NotEqual(HttpStatusCode.TooManyRequests, allowed.StatusCode);
        }

        var rejected = await client.PostFormAsync(LoginForm.Url, token: null);

        Assert.Equal(HttpStatusCode.TooManyRequests, rejected.StatusCode);
        Assert.Equal(MessageTexts.TooManyRequests, await rejected.Content.ReadAsStringAsync());

        // Other pages are not rate limited.
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/Recipes")).StatusCode);
    }
}
