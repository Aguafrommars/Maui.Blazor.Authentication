using Aguacongas.AspNetCore.Components.Maui.Authentication.Oidc.Models;

namespace Aguacongas.AspNetCore.Components.Maui.Authentication.Oidc.Abstraction;

/// <summary>
/// Authentication store interface
/// </summary>
public interface IAuthenticationStore
{
    /// <summary>
    /// Deletes a scope
    /// </summary>
    /// <param name="scope">The scope</param>
    void Delete(string scope);

    /// <summary>
    /// Gets <see cref="AuthenticationEntity"/> for a scope
    /// </summary>
    /// <param name="scope"></param>
    /// <returns>The stored entity</returns>
    Task<AuthenticationEntity> GetAsync(string scope);

    /// <summary>
    /// Sets <see cref="AuthenticationEntity"/> for a scope
    /// </summary>
    /// <param name="scope">The scope</param>
    /// <param name="value">The entity to store</param>
    /// <returns></returns>
    Task SetAsync(string scope, AuthenticationEntity value);
}