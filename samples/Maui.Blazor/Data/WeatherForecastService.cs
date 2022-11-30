using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Shared;
using System.Net.Http.Json;

namespace Maui.Blazor.Data;

public class WeatherForecastService
{
    private readonly HttpClient _http;

    public WeatherForecastService(IHttpClientFactory httpClientFactory)
	{
        _http = httpClientFactory.CreateClient(nameof(WeatherForecastService));
    }

    public async Task<WeatherForecast[]> GetForecastAsync()
	{
        try
        {
            return await _http.GetFromJsonAsync<WeatherForecast[]>("api/WeatherForecast")
                .ConfigureAwait(false);
        }
        catch (AccessTokenNotAvailableException exception)
        {
            exception.Redirect();
        }

        return null;
    }
}

