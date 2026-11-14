using System.Collections.Concurrent;
using System.Net;
using System.Text.RegularExpressions;
using RecipeBook.Services;

namespace Tests.Integration;

public record SentEmail(string To, string Subject, string Body)
{
    /// <summary>
    /// The path and query of the first link in the email, ready to request with the test client.
    /// </summary>
    public string LinkPathAndQuery
    {
        get
        {
            var match = Regex.Match(Body, "href='([^']+)'");
            if (!match.Success)
            {
                throw new InvalidOperationException($"No link in the email '{Subject}' to {To}.");
            }

            return new Uri(WebUtility.HtmlDecode(match.Groups[1].Value)).PathAndQuery;
        }
    }
}

/// <summary>
/// Keeps sent emails in memory instead of sending them, so tests can check them and open their links.
/// </summary>
public class CapturingEmailSender : IEmailSender
{
    private readonly ConcurrentQueue<SentEmail> _sent = new();

    public IReadOnlyList<SentEmail> Sent => _sent.ToList();

    public IReadOnlyList<SentEmail> SentTo(string email)
        => _sent.Where(e => string.Equals(e.To, email, StringComparison.OrdinalIgnoreCase)).ToList();

    public Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        _sent.Enqueue(new SentEmail(email, subject, htmlMessage));
        return Task.CompletedTask;
    }
}
