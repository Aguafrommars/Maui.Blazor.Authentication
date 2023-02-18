using Android.App;
using Android.Content;
using Android.Content.PM;

namespace Maui.Blazor.Client.Platforms.Android;

[Activity(NoHistory = true, LaunchMode = LaunchMode.SingleTop, Exported = true)]
[IntentFilter(new[] { Intent.ActionView },
          Categories = new[] {
            Intent.CategoryDefault,
            Intent.CategoryBrowsable
          },
          DataScheme = CALLBACK_SCHEME,
          DataPaths = new[] {
              "authentication/login-callback",
              "authentication/login-callback"
              })]
public class OidcAuthenticationCallbackActivity : WebAuthenticatorCallbackActivity
{
    const string CALLBACK_SCHEME = "mauiblazorsample";
}
