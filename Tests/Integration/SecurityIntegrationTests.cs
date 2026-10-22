using System.Net;
using System.Net.Http.Headers;
using Microsoft.EntityFrameworkCore;
using RecipeBook.Constants;
using RecipeBook.Models;

namespace Tests.Integration;

/// <summary>
/// End-to-end checks through the real request pipeline: authorization, antiforgery, ownership and redirects.
/// </summary>
public class SecurityIntegrationTests : IClassFixture<RecipeBookFactory>, IAsyncLifetime
{
    private const string DetailsUrl = "/Recipes/Details/100";
    private readonly RecipeBookFactory _factory;

    public SecurityIntegrationTests(RecipeBookFactory factory)
    {
        _factory = factory;
    }

    public Task InitializeAsync() => _factory.SeedAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Theory]
    [InlineData("/")]
    [InlineData("/Recipes")]
    [InlineData(DetailsUrl)]
    [InlineData("/Recipes?page=2&sortOrder=MostLiked")]
    public async Task Anonymous_CanBrowsePublicPages(string url)
    {
        var response = await _factory.CreateClientAs(null).GetAsync(url);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Theory]
    [InlineData("/Recipes/Create")]
    [InlineData("/Recipes/LikedRecipes")]
    [InlineData("/Recipes/Edit/100")]
    [InlineData("/Users")]
    public async Task Anonymous_IsRedirectedToLogin_ForProtectedPages(string url)
    {
        var response = await _factory.CreateClientAs(null).GetAsync(url);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.StartsWith("/Account/Login", response.Headers.Location!.OriginalString);
    }

    [Theory]
    [InlineData("/Ingredients")]
    [InlineData("/RecipeCategories")]
    [InlineData("/AdminStatistics/UserStats")]
    public async Task AdminPages_AreForbiddenForUsers_AndOpenForAdmins(string url)
    {
        var asUser = await _factory.CreateClientAs(RecipeBookFactory.StrangerId, UserRoles.User).GetAsync(url);
        var asAdmin = await _factory.CreateClientAs(RecipeBookFactory.AdminId, UserRoles.Admin).GetAsync(url);

        Assert.Equal(HttpStatusCode.Forbidden, asUser.StatusCode);
        Assert.Equal(HttpStatusCode.OK, asAdmin.StatusCode);
    }

    [Fact]
    public async Task Post_WithoutAntiforgeryToken_IsRejected()
    {
        var client = _factory.CreateClientAs(RecipeBookFactory.StrangerId, UserRoles.User);

        var response = await client.PostFormAsync("/Recipes/Like/100", token: null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.False(await _factory.QueryAsync(db => db.RecipeLikes.AnyAsync(l => l.UserId == RecipeBookFactory.StrangerId)));
    }

    [Fact]
    public async Task Post_WithAntiforgeryToken_Succeeds()
    {
        var client = _factory.CreateClientAs(RecipeBookFactory.StrangerId, UserRoles.User);
        var token = await client.GetAntiforgeryTokenAsync(DetailsUrl);

        var response = await client.PostFormAsync("/Recipes/SaveRecipe/100", token);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.True(await _factory.QueryAsync(db => db.SavedRecipes.AnyAsync(s => s.UserId == RecipeBookFactory.StrangerId && s.RecipeId == 100)));
    }

    [Fact]
    public async Task Like_IgnoresForeignReferer()
    {
        var client = _factory.CreateClientAs(RecipeBookFactory.AdminId, UserRoles.Admin);
        var token = await client.GetAntiforgeryTokenAsync(DetailsUrl);

        var request = new HttpRequestMessage(HttpMethod.Post, "/Recipes/Like/100")
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string> { ["__RequestVerificationToken"] = token })
        };
        request.Headers.Referrer = new Uri("https://evil.example/phish");

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal(DetailsUrl, response.Headers.Location!.OriginalString);
    }

    [Fact]
    public async Task Like_ReturnsToLocalReferer()
    {
        var client = _factory.CreateClientAs(RecipeBookFactory.AuthorId, UserRoles.User);
        var token = await client.GetAntiforgeryTokenAsync(DetailsUrl);

        var request = new HttpRequestMessage(HttpMethod.Post, "/Recipes/Like/100")
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string> { ["__RequestVerificationToken"] = token })
        };
        request.Headers.Referrer = new Uri(client.BaseAddress!, "/Recipes?page=1");

        var response = await client.SendAsync(request);

        Assert.Equal("/Recipes?page=1", response.Headers.Location!.OriginalString);
    }

    [Fact]
    public async Task EditForm_IsForbiddenForOtherUsers_AndOpenForOwnerAndAdmin()
    {
        var asStranger = await _factory.CreateClientAs(RecipeBookFactory.StrangerId, UserRoles.User).GetAsync("/Recipes/Edit/100");
        var asOwner = await _factory.CreateClientAs(RecipeBookFactory.AuthorId, UserRoles.User).GetAsync("/Recipes/Edit/100");
        var asAdmin = await _factory.CreateClientAs(RecipeBookFactory.AdminId, UserRoles.Admin).GetAsync("/Recipes/Edit/100");

        Assert.Equal(HttpStatusCode.Forbidden, asStranger.StatusCode);
        Assert.Equal(HttpStatusCode.OK, asOwner.StatusCode);
        Assert.Equal(HttpStatusCode.OK, asAdmin.StatusCode);
    }

    [Fact]
    public async Task EditPost_ByOtherUser_IsForbidden_AndChangesNothing()
    {
        var client = _factory.CreateClientAs(RecipeBookFactory.StrangerId, UserRoles.User);
        var token = await client.GetAntiforgeryTokenAsync(DetailsUrl);

        var response = await client.PostAsync("/Recipes/Edit/100", RecipeForm(token, title: "Hacked"));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("Боб чорба", await _factory.QueryAsync(db => db.Recipes.Where(r => r.Id == 100).Select(r => r.Title).SingleAsync()));
    }

    [Fact]
    public async Task EditPost_ByOwner_SavesChanges()
    {
        await AddRecipeAsync(201, RecipeBookFactory.AuthorId);
        var client = _factory.CreateClientAs(RecipeBookFactory.AuthorId, UserRoles.User);
        var token = await client.GetAntiforgeryTokenAsync("/Recipes/Edit/201");

        var response = await client.PostAsync("/Recipes/Edit/201", RecipeForm(token, id: 201, title: "Нова чорба"));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("Нова чорба", await _factory.QueryAsync(db => db.Recipes.Where(r => r.Id == 201).Select(r => r.Title).SingleAsync()));
    }

    [Fact]
    public async Task EditPost_RejectsNonImageUpload()
    {
        await AddRecipeAsync(202, RecipeBookFactory.AuthorId);
        var client = _factory.CreateClientAs(RecipeBookFactory.AuthorId, UserRoles.User);
        var token = await client.GetAntiforgeryTokenAsync("/Recipes/Edit/202");

        var form = RecipeForm(token, id: 202, title: "С картинка");
        var file = new ByteArrayContent("<html><script>alert(1)</script></html>"u8.ToArray());
        file.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        form.Add(file, "imageFileNew", "photo.png");

        var response = await client.PostAsync("/Recipes/Edit/202", form);

        // The form is shown again with a validation error; nothing is saved.
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var saved = await _factory.QueryAsync(db => db.Recipes.AsNoTracking().SingleAsync(r => r.Id == 202));
        Assert.Null(saved.ImageUrl);
        Assert.NotEqual("С картинка", saved.Title);
    }

    [Fact]
    public async Task Delete_ByOtherUser_IsForbidden_ButAdminCanDelete()
    {
        await AddRecipeAsync(203, RecipeBookFactory.AuthorId);

        var stranger = _factory.CreateClientAs(RecipeBookFactory.StrangerId, UserRoles.User);
        var strangerToken = await stranger.GetAntiforgeryTokenAsync(DetailsUrl);
        var strangerResponse = await stranger.PostFormAsync("/Recipes/Delete/203", strangerToken);

        Assert.Equal(HttpStatusCode.Forbidden, strangerResponse.StatusCode);
        Assert.True(await _factory.QueryAsync(db => db.Recipes.AnyAsync(r => r.Id == 203)));

        var admin = _factory.CreateClientAs(RecipeBookFactory.AdminId, UserRoles.Admin);
        var adminToken = await admin.GetAntiforgeryTokenAsync(DetailsUrl);
        var adminResponse = await admin.PostFormAsync("/Recipes/Delete/203", adminToken);

        Assert.Equal(HttpStatusCode.Redirect, adminResponse.StatusCode);
        Assert.False(await _factory.QueryAsync(db => db.Recipes.AnyAsync(r => r.Id == 203)));
    }

    private static MultipartFormDataContent RecipeForm(string token, int id = 100, string title = "Боб чорба")
    {
        return new MultipartFormDataContent
        {
            { new StringContent(token), "__RequestVerificationToken" },
            { new StringContent(id.ToString()), "Id" },
            { new StringContent(title), "Title" },
            { new StringContent("<p>Описание</p>"), "Description" },
            { new StringContent("<p>Сварете</p>"), "Instructions" },
            { new StringContent("4"), "Servings" },
            { new StringContent("60"), "CookingTime" },
            { new StringContent(RecipeBookFactory.CategoryId.ToString()), "CategoryId" },
            { new StringContent("[]"), "SelectedIngredientsJson" }
        };
    }

    private Task AddRecipeAsync(int id, string userId)
        => _factory.QueryAsync(async db =>
        {
            db.Recipes.Add(new Recipe
            {
                Id = id,
                UserId = userId,
                Title = $"Рецепта {id}",
                Description = "<p>Описание</p>",
                Instructions = "<p>Стъпки</p>",
                Servings = 2,
                CookingTime = 20,
                RecipeCategoryId = RecipeBookFactory.CategoryId
            });
            return await db.SaveChangesAsync();
        });
}
