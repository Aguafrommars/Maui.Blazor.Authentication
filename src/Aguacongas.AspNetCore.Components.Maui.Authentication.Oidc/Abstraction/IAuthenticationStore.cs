using Aguacongas.AspNetCore.Components.Maui.Authentication.Oidc.Models;

namespace Aguacongas.AspNetCore.Components.Maui.Authentication.Oidc.Abstraction;

public interface IAuthenticationStore
{
    void Delete(string scope);
    Task<AuthenticationEntity> GetAsync(string scope);
    Task SetAsync(string scope, AuthenticationEntity value);
}