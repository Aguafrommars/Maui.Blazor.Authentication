using Aguacongas.AspNetCore.Components.Maui.Authentication.Oidc.Abstraction;
using Aguacongas.AspNetCore.Components.Maui.Authentication.Oidc.Models;
using System.Text.Json;

namespace Aguacongas.AspNetCore.Components.Maui.Authentication.Oidc.Services;

internal class AuthenticationStore : IAuthenticationStore
{
    private readonly ISecureStorage _secureStorage;

    private static string GetKey(string scope) => $"{typeof(AuthenticationEntity).Name}-{scope.Replace(' ', '-')}";

    public AuthenticationStore(ISecureStorage secureStorage)
    {
        _secureStorage = secureStorage;
    }

    public async Task<AuthenticationEntity> GetAsync(string scope)
    {
        var value = await _secureStorage.GetAsync(GetKey(scope)).ConfigureAwait(false);
        if (value is null)
        {
            return null;
        }
        return JsonSerializer.Deserialize<AuthenticationEntity>(value);
    }

    public Task SetAsync(string scope, AuthenticationEntity value)
    => _secureStorage.SetAsync(GetKey(scope), JsonSerializer.Serialize(value));

    public void Delete(string scope) => _secureStorage.Remove(GetKey(scope));
}