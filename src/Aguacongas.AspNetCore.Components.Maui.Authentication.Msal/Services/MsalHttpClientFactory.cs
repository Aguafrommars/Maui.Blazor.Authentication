using Microsoft.Identity.Client;

namespace Aguacongas.AspNetCore.Components.Maui.Authentication.Msal.Services;

internal class MsalHttpClientFactory : IMsalHttpClientFactory
{
    private readonly HttpClient _httpClient;

    public MsalHttpClientFactory(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public HttpClient GetHttpClient()
    => _httpClient;
}
