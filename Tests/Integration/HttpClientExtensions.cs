using System.Text.RegularExpressions;

namespace Tests.Integration;

internal static partial class HttpClientExtensions
{
    [GeneratedRegex("name=\"__RequestVerificationToken\" type=\"hidden\" value=\"([^\"]+)\"")]
    private static partial Regex AntiforgeryTokenRegex();

    /// <summary>
    /// Client that acts as the given user (or anonymous when null) and does not follow redirects,
    /// so tests can assert on 302 responses and their Location.
    /// </summary>
    public static HttpClient CreateClientAs(this RecipeBookFactory factory, string? userId, params string[] roles)
    {
        var client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        if (userId != null)
        {
            client.DefaultRequestHeaders.Add(TestAuthHandler.UserHeader, userId);
        }

        if (roles.Length > 0)
        {
            client.DefaultRequestHeaders.Add(TestAuthHandler.RolesHeader, string.Join(',', roles));
        }

        return client;
    }

    /// <summary>
    /// Loads a page and returns the antiforgery token from its first form. The client keeps the matching cookie.
    /// </summary>
    public static async Task<string> GetAntiforgeryTokenAsync(this HttpClient client, string pageUrl)
    {
        var response = await client.GetAsync(pageUrl);
        response.EnsureSuccessStatusCode();
        var html = await response.Content.ReadAsStringAsync();

        var match = AntiforgeryTokenRegex().Match(html);
        if (!match.Success)
        {
            throw new InvalidOperationException($"No antiforgery token found on {pageUrl}.");
        }

        return match.Groups[1].Value;
    }

    public static Task<HttpResponseMessage> PostFormAsync(this HttpClient client, string url, string? token, params (string Key, string Value)[] fields)
    {
        var values = fields.Select(f => new KeyValuePair<string, string>(f.Key, f.Value)).ToList();
        if (token != null)
        {
            values.Add(new("__RequestVerificationToken", token));
        }

        return client.PostAsync(url, new FormUrlEncodedContent(values));
    }
}
