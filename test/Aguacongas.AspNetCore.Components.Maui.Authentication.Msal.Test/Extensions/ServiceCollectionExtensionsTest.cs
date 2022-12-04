using Aguacongas.AspNetCore.Components.Maui.Authentication.Services;
using Aguacongas.AspNetCore.Components.Maui.Authentication.Test.Utils;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.Identity.Client;

namespace Aguacongas.AspNetCore.Components.Maui.Authentication.Test.Extensions;

public class ServiceCollectionExtensionsTest
{
    [Fact]
    public void AddMauiOidcAuthentication_should_add_required_service()
    {
        var services = new ServiceCollection();
        services.AddTransient<NavigationManager, FakeNavigationManager>()
            .AddMauiOidcAuthentication(options =>
        {
            var providerOptions = options.ProviderOptions;
            providerOptions.Authority = "https://exemple.com";
            providerOptions.RedirectUri = "test://authentication/login-callback";
            providerOptions.PostLogoutRedirectUri = "test://authentication/logout-callback";
        });

        var provider = services.BuildServiceProvider();

        Assert.IsType<OidcAuthenticationService<RemoteAuthenticationState>>(provider.GetService<AuthenticationStateProvider>());
        Assert.NotNull(provider.GetService<IWebAuthenticator>());
        Assert.NotNull(provider.GetService<ISecureStorage>());
        Assert.NotNull(provider.GetService<IPublicClientApplication>());
    }

    [Fact]
    public void AddMauiOidcAuthentication_should_configure_http_message_builder()
    {
        var called = false;
        var services = new ServiceCollection();
        services.AddTransient<NavigationManager, FakeNavigationManager>()
            .AddMauiOidcAuthentication(options =>
            {
                var providerOptions = options.ProviderOptions;
                providerOptions.Authority = "https://exemple.com";
                providerOptions.RedirectUri = "test://authentication/login-callback";
                providerOptions.PostLogoutRedirectUri = "test://authentication/logout-callback";
            }, builder =>
            {
                called = true;
            });

        var provider = services.BuildServiceProvider();

        var oidcClient = provider.GetRequiredService<IPublicClientApplication>();
        var httpClient = oidcClient.AppConfig.HttpClientFactory.GetHttpClient();
        Assert.NotNull(httpClient);

        Assert.True(called);
    }
}
