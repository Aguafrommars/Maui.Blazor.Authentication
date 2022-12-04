using Duende.IdentityServer.Models;

namespace OidcAndApiServer;

public static class Config
{
    public static IEnumerable<IdentityResource> IdentityResources =>
        new IdentityResource[]
        {
            new IdentityResources.OpenId(),
            new IdentityResources.Profile(),
        };

    public static IEnumerable<ApiScope> ApiScopes =>
        new ApiScope[]
        {
            new ApiScope("scope1"),
        };

    public static IEnumerable<ApiResource> Apis =>
        new ApiResource[]
        {
            new ApiResource("api")
            {
                Scopes = new[]
                {
                    "scope1"
                }
            },
        };

    public static IEnumerable<Client> Clients =>
        new Client[]
        {
            // Maui Blazor sample
            new Client
            {
                ClientId = "mauiblazorsample",
                RequirePkce = true,
                RequireClientSecret = false,

                AllowedGrantTypes = GrantTypes.Code,

                RedirectUris = { "mauiblazorsample://authentication/login-callback" },
                PostLogoutRedirectUris = { "mauiblazorsample://authentication/logout-callback" },

                AllowOfflineAccess = true,
                AllowedScopes = { "openid", "profile", "scope1" }
            },
        };
}
