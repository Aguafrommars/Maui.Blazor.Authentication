using Aguacongas.AspNetCore.Components.Maui.Authentication.Oidc.Abstraction;
using Aguacongas.AspNetCore.Components.Maui.Authentication.Oidc.Services;
using IdentityModel.OidcClient;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Options;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Contains extension methods to add authentication to Maui Blazor applications.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds support for authentication for SPA applications using <see cref="OidcProviderOptions"/> and the <see cref="RemoteAuthenticationState"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
    /// <param name="configure">An action that will configure the <see cref="RemoteAuthenticationOptions{TProviderOptions}"/>.</param>
    /// <param name="configureBuilder">An action that will configure the <see cref="HttpMessageHandlerBuilder"/>.</param>
    /// <returns>The <see cref="IServiceCollection"/> where the services were registered.</returns>
    public static IRemoteAuthenticationBuilder<RemoteAuthenticationState, RemoteUserAccount> AddMauiOidcAuthentication(
        this IServiceCollection services, Action<RemoteAuthenticationOptions<OidcProviderOptions>> configure,
        Action<HttpMessageHandlerBuilder> configureBuilder = null)
    {
        return AddMauiOidcAuthentication<RemoteAuthenticationState>(services, configure, configureBuilder);
    }

    /// <summary>
    /// Adds support for authentication for SPA applications using <see cref="OidcProviderOptions"/> and the <see cref="RemoteAuthenticationState"/>.
    /// </summary>
    /// <typeparam name="TRemoteAuthenticationState">The type of the remote authentication state.</typeparam>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
    /// <param name="configure">An action that will configure the <see cref="RemoteAuthenticationOptions{TProviderOptions}"/>.</param>
    /// <param name="configureBuilder">An action that will configure the <see cref="HttpMessageHandlerBuilder"/>.</param>
    /// <returns>The <see cref="IServiceCollection"/> where the services were registered.</returns>
    public static IRemoteAuthenticationBuilder<TRemoteAuthenticationState, RemoteUserAccount> AddMauiOidcAuthentication<TRemoteAuthenticationState>(
        this IServiceCollection services, Action<RemoteAuthenticationOptions<OidcProviderOptions>> configure,
        Action<HttpMessageHandlerBuilder> configureBuilder = null)
        where TRemoteAuthenticationState : RemoteAuthenticationState, new()
    {
        return AddMauiOidcAuthentication<TRemoteAuthenticationState, RemoteUserAccount>(services, configure, configureBuilder);
    }

    /// <summary>
    /// Adds support for authentication for SPA applications using <see cref="OidcProviderOptions"/> and the <see cref="RemoteAuthenticationState"/>.
    /// </summary>
    /// <typeparam name="TRemoteAuthenticationState">The type of the remote authentication state.</typeparam>
    /// <typeparam name="TAccount">The account type.</typeparam>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
    /// <param name="configure">An action that will configure the <see cref="RemoteAuthenticationOptions{TProviderOptions}"/>.</param>
    /// <param name="configureBuilder">An action that will configure the <see cref="HttpMessageHandlerBuilder"/>.</param>
    /// <returns>The <see cref="IServiceCollection"/> where the services were registered.</returns>
    public static IRemoteAuthenticationBuilder<TRemoteAuthenticationState, TAccount>
        AddMauiOidcAuthentication<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors |
            DynamicallyAccessedMemberTypes.PublicFields |
            DynamicallyAccessedMemberTypes.PublicProperties)] TRemoteAuthenticationState,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors |
            DynamicallyAccessedMemberTypes.PublicFields |
            DynamicallyAccessedMemberTypes.PublicProperties)] TAccount>(this IServiceCollection services,
            Action<RemoteAuthenticationOptions<OidcProviderOptions>> configure,
            Action<HttpMessageHandlerBuilder> configureBuilder = null)
        where TRemoteAuthenticationState : RemoteAuthenticationState, new()
        where TAccount : RemoteUserAccount
    {
        services.TryAddScoped<AuthenticationStateProvider, OidcAuthenticationService<TRemoteAuthenticationState>>();
        services.TryAddTransient<IAuthenticationStore, AuthenticationStore>();
#if WINDOWS
	    services.AddScoped<IWebAuthenticator>(p => WinUIEx.WebAuthenticator.Instance);
#else
        services.TryAddScoped(sp => WebAuthenticator.Default);
#endif

        services.TryAddScoped(sp => SecureStorage.Default);
        services.TryAddScoped(sp =>
        {
            var settings = sp.GetRequiredService<IOptions<RemoteAuthenticationOptions<OidcProviderOptions>>>().Value;
            var providerOptions = settings.ProviderOptions;
            var options = new OidcClientOptions
            {
                Authority = providerOptions.Authority,
                Browser = new WebBrowserAuthenticator(sp.GetRequiredService<IWebAuthenticator>()),
                ClientId = providerOptions.ClientId,
                Scope = string.Join(" ", providerOptions.DefaultScopes),
                RedirectUri = providerOptions.RedirectUri,
                PostLogoutRedirectUri = providerOptions.PostLogoutRedirectUri,
                RefreshDiscoveryDocumentForLogin = false,
                HttpClientFactory =
                    o => sp.GetRequiredService<IHttpClientFactory>().CreateClient(nameof(OidcClient))
            };
            return new OidcClient(options);
        });

        services.AddHttpClient(nameof(OidcClient))
            .ConfigureHttpMessageHandlerBuilder(builder =>
            {
                if (configureBuilder is not null)
                {
                    configureBuilder(builder);
                }
            });

        return services.AddOidcAuthentication<TRemoteAuthenticationState, TAccount>(configure);
    }
}