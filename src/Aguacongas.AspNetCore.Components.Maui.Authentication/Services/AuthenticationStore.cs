using Aguacongas.AspNetCore.Components.Maui.Authentication.Abstraction;
using Aguacongas.AspNetCore.Components.Maui.Authentication.Models;
using System.Text.Json;

namespace Aguacongas.AspNetCore.Components.Maui.Authentication.Services;

internal class AuthenticationStore : IAuthenticationStore
{
    private static string GetKey(string scope) => $"{typeof(AuthenticationEntity).Name}-{scope.Replace(' ', '-')}";

    public async Task<AuthenticationEntity> GetAsync(string scope)
    {
        var value = await SecureStorage.Default.GetAsync(GetKey(scope)).ConfigureAwait(false);
        if (value is null)
        {
            return null;
        }
        return JsonSerializer.Deserialize<AuthenticationEntity>(value);
    }

    public Task SetAsync(string scope, AuthenticationEntity value)
    => SecureStorage.Default.SetAsync(GetKey(scope), JsonSerializer.Serialize(value));

    public void Delete(string scope) => SecureStorage.Default.Remove(GetKey(scope));
}