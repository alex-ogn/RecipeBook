using System.Net;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using RecipeBook.Constants;

namespace Tests.Integration;

/// <summary>
/// Profile pictures are files under wwwroot/images/profiles: uploading through the profile form, who may
/// change a picture, how the file is served, and that replaced or deleted pictures leave no files behind.
/// </summary>
public class ProfilePictureIntegrationTests : IClassFixture<RecipeBookFactory>, IAsyncLifetime
{
    private static readonly byte[] PngBytes = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0, 0, 0, 0 };
    private readonly RecipeBookFactory _factory;

    public ProfilePictureIntegrationTests(RecipeBookFactory factory)
    {
        _factory = factory;
    }

    public Task InitializeAsync() => _factory.SeedAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Owner_UploadsTwice_OnlyTheNewFileIsKept_AndItIsServedWithLongCache()
    {
        var user = await _factory.CreateLoginUserAsync("avatar-owner", LoginForm.Password);
        var client = _factory.CreateClientAs(user.Id, UserRoles.User);

        var first = await UploadAsync(client, user.Id, user.UserName!);
        var second = await UploadAsync(client, user.Id, user.UserName!);

        Assert.NotEqual(first, second);
        Assert.False(File.Exists(PhysicalPath(first)));
        Assert.True(File.Exists(PhysicalPath(second)));

        // The profile page shows the file itself; there is no per-avatar controller action any more.
        var profile = await client.GetStringAsync($"/Recipes/MyIndex/{user.Id}");
        Assert.Contains(second, profile);

        var image = await client.GetAsync(second);
        Assert.Equal(HttpStatusCode.OK, image.StatusCode);
        Assert.True(image.Headers.CacheControl?.Public);
        Assert.Equal(TimeSpan.FromSeconds(ImageConstants.UploadedImageCacheSeconds), image.Headers.CacheControl?.MaxAge);

        File.Delete(PhysicalPath(second));
    }

    [Fact]
    public async Task OtherUser_CannotChangeSomeoneElsesPicture()
    {
        var victim = await _factory.CreateLoginUserAsync("avatar-victim", LoginForm.Password);
        var stranger = _factory.CreateClientAs(RecipeBookFactory.StrangerId, UserRoles.User);
        var token = await stranger.GetAntiforgeryTokenAsync($"/Users/Edit/{RecipeBookFactory.StrangerId}");

        var response = await stranger.PostAsync("/Users/Edit", ProfileForm(token, victim.Id, victim.UserName!));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Null((await _factory.FindUserAsync(victim.Id))!.ProfilePictureUrl);
    }

    [Fact]
    public async Task Anonymous_CannotUploadAPicture()
    {
        var response = await _factory.CreateClientAs(null)
            .PostAsync("/Users/Edit", ProfileForm(token: null, RecipeBookFactory.AuthorId, RecipeBookFactory.AuthorId));

        // Sent to the login page (or refused for the missing token), never saved.
        var sentToLogin = response.StatusCode == HttpStatusCode.Redirect
            && response.Headers.Location!.OriginalString.Contains(CookieConstants.LoginPath);
        Assert.True(sentToLogin || response.StatusCode == HttpStatusCode.BadRequest, response.StatusCode.ToString());
        Assert.Null((await _factory.FindUserAsync(RecipeBookFactory.AuthorId))!.ProfilePictureUrl);
    }

    [Fact]
    public async Task DeletingTheAccount_DeletesThePictureFile()
    {
        var user = await _factory.CreateLoginUserAsync("avatar-delete", LoginForm.Password);
        var client = _factory.CreateClientAs(user.Id, UserRoles.User);
        var url = await UploadAsync(client, user.Id, user.UserName!);

        var response = await client.PostFormAsync("/Account/DeleteAccount",
            await client.GetAntiforgeryTokenAsync("/Account/DeleteAccount"),
            ("CurrentPassword", LoginForm.Password), ("Confirm", "true"));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.False(File.Exists(PhysicalPath(url)));
    }

    /// <summary>
    /// Posts the profile form with a PNG and returns the picture URL the user row now holds.
    /// </summary>
    private async Task<string> UploadAsync(HttpClient client, string userId, string userName)
    {
        var token = await client.GetAntiforgeryTokenAsync($"/Users/Edit/{userId}");
        var response = await client.PostAsync("/Users/Edit", ProfileForm(token, userId, userName));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var url = (await _factory.FindUserAsync(userId))!.ProfilePictureUrl;
        Assert.NotNull(url);
        Assert.StartsWith(ImageConstants.ProfileImagesUrlPrefix, url);
        return url;
    }

    private static MultipartFormDataContent ProfileForm(string? token, string userId, string userName)
    {
        var form = new MultipartFormDataContent();
        if (token != null)
        {
            form.Add(new StringContent(token), "__RequestVerificationToken");
        }

        form.Add(new StringContent(userId), "Id");
        form.Add(new StringContent(userName), "UserName");

        var file = new ByteArrayContent(PngBytes);
        file.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        form.Add(file, "ProfilePicture", "me.png");
        return form;
    }

    private string PhysicalPath(string url)
    {
        var webRoot = _factory.Services.GetRequiredService<IWebHostEnvironment>().WebRootPath;
        return Path.Combine(webRoot, ImageConstants.ImagesFolder, ImageConstants.ProfileImagesFolder, Path.GetFileName(url));
    }
}
