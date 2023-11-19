using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;

namespace Maui.Blazor.Client;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
            });

        var services = builder.Services;
        services.AddMauiBlazorWebView();

#if DEBUG
        services.AddBlazorWebViewDeveloperTools();
        builder.Logging.AddDebug();
#endif

        string authorityUrl =
            DeviceInfo.Platform == DevicePlatform.Android ? "https://10.0.2.2:5001" : "https://localhost:5001";

        services.AddMauiOidcAuthentication(options =>
            {
                var providerOptions = options.ProviderOptions;
                providerOptions.Authority = authorityUrl;
                providerOptions.ClientId = "mauiblazorsample";
                providerOptions.RedirectUri = "mauiblazorsample://authentication/login-callback";
                providerOptions.PostLogoutRedirectUri = "mauiblazorsample://authentication/logout-callback";
                providerOptions.DefaultScopes.Add("offline_access");
                providerOptions.DefaultScopes.Add("scope1");
            }, GetHttpMessgeHandler);

        services.AddDefaultHttpClient(authorityUrl, GetHttpMessgeHandler);

        return builder.Build();
    }

    private static HttpMessageHandler GetHttpMessgeHandler(IServiceProvider provider)
    {
#if IOS
        return new NSUrlSessionHandler
        {
            TrustOverrideForUrl = (sender, url, trust) =>
            {
                if (url.StartsWith("https://localhost:5001"))
                {
                    return true;
                }
                return false;
            }
        };
#else
        var handler = provider.GetService<IHttpMessageHandlerFactory>()?.CreateHandler() as HttpClientHandler ?? new HttpClientHandler();
        handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) =>
        {
            if (cert != null && cert.Issuer.Equals("CN=localhost"))
            {
                return true;
            }
            return errors == System.Net.Security.SslPolicyErrors.None;
        };
        return handler;
#endif
    }
}
