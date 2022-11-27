using Aguacongas.AspNetCore.Components.Maui.Authentication.Models;

namespace Aguacongas.AspNetCore.Components.Maui.Authentication.Abstraction;

public interface IAuthenticationStore
{
    void Delete(string scope);
    Task<AuthenticationEntity> GetAsync(string scope);
    Task SetAsync(string scope, AuthenticationEntity value);
}