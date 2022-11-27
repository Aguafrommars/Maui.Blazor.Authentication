using IdentityModel.Client;
using IdentityModel.OidcClient.Browser;
using IBrowser = IdentityModel.OidcClient.Browser.IBrowser;

namespace Aguacongas.AspNetCore.Components.Maui.Authentication.Services;

public class WebBrowserAuthenticator : IBrowser
{
    public async Task<BrowserResult> InvokeAsync(BrowserOptions options, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await WebAuthenticator.AuthenticateAsync(new WebAuthenticatorOptions
            {
                Url = new Uri(options.StartUrl),
                CallbackUrl = new Uri(options.EndUrl),
                PrefersEphemeralWebBrowserSession = true
            }).ConfigureAwait(false);

            var url = new RequestUrl(options.EndUrl)
                .Create(new Parameters(result.Properties));

            return new BrowserResult
            {
                Response = url,
                ResultType = BrowserResultType.Success
            };
        }
        catch (TaskCanceledException)
        {
            return new BrowserResult
            {
                ResultType = BrowserResultType.UserCancel,
                ErrorDescription = "Canceled by the user."
            };
        }
    }
}