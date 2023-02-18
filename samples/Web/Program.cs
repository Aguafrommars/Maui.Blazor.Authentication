using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Web.Blazor.Client;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

var autorityUrl = "https://localhost:5001";

var services = builder.Services;

services.AddOidcAuthentication(options =>
{
    var providerOptions = options.ProviderOptions;
    providerOptions.Authority = autorityUrl;
    providerOptions.ClientId = "mauiblazorsample";
    providerOptions.ResponseType = "code";
    providerOptions.RedirectUri = "https://localhost:7043/authentication/login-callback";
    providerOptions.PostLogoutRedirectUri = "https://localhost:7043/authentication/logout-callback";
    providerOptions.DefaultScopes.Add("offline_access");
    providerOptions.DefaultScopes.Add("scope1");
});

services.AddDefaultHttpClient(autorityUrl);

await builder.Build().RunAsync();
