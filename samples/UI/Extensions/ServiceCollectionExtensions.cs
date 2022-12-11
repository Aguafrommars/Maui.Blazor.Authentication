using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.Extensions.Http;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDefaultHttpClient(this IServiceCollection services, string authorityUrl, Action<HttpMessageHandlerBuilder>? configureBuilder = null)
    {
        services.AddTransient(p =>
        {
            var handler = new AuthorizationMessageHandler(p.GetRequiredService<IAccessTokenProvider>(),
                p.GetRequiredService<NavigationManager>());

            handler.ConfigureHandler(new[]
            {
                    authorityUrl
            });
            return handler;
        })
        .AddTransient(p => p.GetRequiredService<IHttpClientFactory>().CreateClient("default"))
        .AddHttpClient("default")
        .ConfigureHttpClient(client => client.BaseAddress = new Uri(authorityUrl))
        .AddHttpMessageHandler<AuthorizationMessageHandler>()
        .ConfigureHttpMessageHandlerBuilder(builder =>
        {
            if (configureBuilder is not null)
            {
                configureBuilder(builder);
            }
        });

        return services;
    }
}
