using Microsoft.AspNetCore.Mvc;

namespace RecipeBook.Extensions
{
    public static class ControllerExtensions
    {
        /// <summary>
        /// Redirects back to the page that submitted the request, but only when the Referer points to this site;
        /// otherwise redirects to <paramref name="fallbackUrl"/>. Prevents open redirects through a forged Referer.
        /// </summary>
        public static IActionResult RedirectToLocalReferer(this Controller controller, string fallbackUrl)
        {
            var referer = controller.Request.Headers.Referer.ToString();

            if (Uri.TryCreate(referer, UriKind.Absolute, out var uri)
                && string.Equals(uri.Authority, controller.Request.Host.Value, StringComparison.OrdinalIgnoreCase))
            {
                var localUrl = uri.PathAndQuery + uri.Fragment;
                if (controller.Url.IsLocalUrl(localUrl))
                {
                    return controller.LocalRedirect(localUrl);
                }
            }

            return controller.LocalRedirect(fallbackUrl);
        }
    }
}
