using System.Net;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using RecipeBook.Constants;
using RecipeBook.Data;
using RecipeBook.Models;
using RecipeBook.Resources;

namespace Tests.Integration;

/// <summary>
/// Pages and links that connect the app together: access denied, logout and the way back from editing a comment.
/// </summary>
public class NavigationIntegrationTests : IClassFixture<RecipeBookFactory>, IAsyncLifetime
{
    private const int CommentId = 500;
    private readonly RecipeBookFactory _factory;

    public NavigationIntegrationTests(RecipeBookFactory factory)
    {
        _factory = factory;
    }

    public async Task InitializeAsync()
    {
        await _factory.SeedAsync();

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        if (await context.RecipeComments.FindAsync(CommentId) == null)
        {
            context.RecipeComments.Add(new RecipeComment
            {
                Id = CommentId,
                Content = "Вкусно!",
                UserId = RecipeBookFactory.StrangerId,
                RecipeId = RecipeBookFactory.AuthorRecipeId
            });
            await context.SaveChangesAsync();
        }
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task AccessDeniedPath_OfTheAuthCookie_IsARealPage()
    {
        // The path the cookie middleware redirects forbidden users to must exist (it used to be a 404).
        var cookieOptions = _factory.Services
            .GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>()
            .Get(IdentityConstants.ApplicationScheme);
        Assert.Equal(CookieConstants.AccessDeniedPath, cookieOptions.AccessDeniedPath.Value);

        var response = await _factory.CreateClientAs(RecipeBookFactory.StrangerId, UserRoles.User)
            .GetAsync(cookieOptions.AccessDeniedPath.Value);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains(MessageTexts.AccessDeniedTitle, WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync()));
    }

    [Fact]
    public async Task EditCommentPage_LinksBackToItsRecipe()
    {
        var response = await _factory.CreateClientAs(RecipeBookFactory.StrangerId, UserRoles.User)
            .GetAsync($"/Recipes/EditComment/{CommentId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains($"href=\"/Recipes/Details/{RecipeBookFactory.AuthorRecipeId}\"", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task EditCommentPost_WithInvalidText_StillLinksBackToItsRecipe()
    {
        var client = _factory.CreateClientAs(RecipeBookFactory.StrangerId, UserRoles.User);
        var token = await client.GetAntiforgeryTokenAsync($"/Recipes/EditComment/{CommentId}");

        // A wrong RecipeId in the form must be ignored: the link comes from the database.
        var response = await client.PostFormAsync("/Recipes/EditComment", token,
            ("Id", CommentId.ToString()), ("RecipeId", "999"), ("Content", "x"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains($"href=\"/Recipes/Details/{RecipeBookFactory.AuthorRecipeId}\"", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task EditCommentPage_IsForbiddenForOtherUsers()
    {
        var response = await _factory.CreateClientAs(RecipeBookFactory.AuthorId, UserRoles.User)
            .GetAsync($"/Recipes/EditComment/{CommentId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Menu_LogoutForm_PostsToTheAccountController()
    {
        var response = await _factory.CreateClientAs(RecipeBookFactory.AuthorId, UserRoles.User).GetAsync("/");

        Assert.Contains($"action=\"{CookieConstants.LogoutPath}\"", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Logout_WithToken_RedirectsHome()
    {
        var client = _factory.CreateClientAs(RecipeBookFactory.AuthorId, UserRoles.User);
        var token = await client.GetAntiforgeryTokenAsync("/");

        var response = await client.PostFormAsync(CookieConstants.LogoutPath, token);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/", response.Headers.Location!.OriginalString);
    }

    [Fact]
    public async Task Logout_WithoutToken_IsRejected()
    {
        var response = await _factory.CreateClientAs(RecipeBookFactory.AuthorId, UserRoles.User)
            .PostFormAsync(CookieConstants.LogoutPath, token: null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
