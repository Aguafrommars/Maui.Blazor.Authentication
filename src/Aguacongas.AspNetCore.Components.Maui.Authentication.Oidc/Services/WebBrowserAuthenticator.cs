using IdentityModel.Client;
using IdentityModel.OidcClient.Browser;
using IBrowser = IdentityModel.OidcClient.Browser.IBrowser;

namespace Aguacongas.AspNetCore.Components.Maui.Authentication.Oidc.Services;

/// <summary>
/// Web browser authenticator
/// </summary>
public class WebBrowserAuthenticator : IBrowser
{
    private readonly IWebAuthenticator _authenticator;

    /// <summary>
    /// Initialize a new instance of <see cref="WebBrowserAuthenticator"/>
    /// </summary>
    /// <param name="authenticator"></param>
    public WebBrowserAuthenticator(IWebAuthenticator authenticator)
    {
        _authenticator = authenticator;
    }

    /// <inheritdoc/>
    public async Task<BrowserResult> InvokeAsync(BrowserOptions options, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _authenticator.AuthenticateAsync(new WebAuthenticatorOptions
            {
                Url = new Uri(options.StartUrl),
                CallbackUrl = new Uri(options.EndUrl),
                PrefersEphemeralWebBrowserSession = true
            }).ConfigureAwait(false);

#if WINDOWS
            var url = ToRawIdentityUrl(options.EndUrl, result);
#else
            var url = new RequestUrl(options.EndUrl)
                .Create(new Parameters(result.Properties));
#endif
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

#if WINDOWS
    private static string ToRawIdentityUrl(string redirectUrl, WebAuthenticatorResult result)
    {
        var parameters = result.Properties.Select(pair => $"{pair.Key}={pair.Value}");
        var modifiedParameters = parameters.ToList();

        var stateParameter = modifiedParameters
            .FirstOrDefault(p => p.StartsWith("state", StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrWhiteSpace(stateParameter))
        {
            // Remove the state key added by WebAuthenticator that includes appInstanceId
            modifiedParameters = modifiedParameters.Where(p => !p.StartsWith("state", StringComparison.OrdinalIgnoreCase)).ToList();

            stateParameter = System.Web.HttpUtility.UrlDecode(stateParameter).Split('&').Last();
            modifiedParameters.Add(stateParameter);
        }
        var values = string.Join("&", modifiedParameters);
     
        return $"{redirectUrl}#{values}";
    }
#endif
}