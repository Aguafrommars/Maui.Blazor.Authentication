using Aguacongas.AspNetCore.Components.Maui.Authentication.Oidc.Abstraction;
using Aguacongas.AspNetCore.Components.Maui.Authentication.Oidc.Models;
using System.Text.Json;

namespace Aguacongas.AspNetCore.Components.Maui.Authentication.Oidc.Services;

internal class AuthenticationStore(ISecureStorage secureStorage) : IAuthenticationStore
{
    private static string GetKey(string scope) => $"{typeof(AuthenticationEntity).Name}-{scope.Replace(' ', '-')}";

    public async Task<AuthenticationEntity> GetAsync(string scope)
    {
        var value = await secureStorage.GetAsync(GetKey(scope)).ConfigureAwait(false);
        if (value is null)
        {
            return null;
        }
        return JsonSerializer.Deserialize(value, AuthenticationEntityJsonContext.Default.AuthenticationEntity);
    }

    public Task SetAsync(string scope, AuthenticationEntity value)
    => secureStorage.SetAsync(GetKey(scope), JsonSerializer.Serialize(value, AuthenticationEntityJsonContext.Default.AuthenticationEntity));

    public void Delete(string scope) => secureStorage.Remove(GetKey(scope));
}