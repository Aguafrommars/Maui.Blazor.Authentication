namespace Aguacongas.AspNetCore.Components.Maui.Authentication.Oidc.Models;

/// <summary>
/// Define an authentication
/// </summary>
public record AuthenticationEntity
{
    /// <summary>
    /// Collection of claims
    /// </summary>
    public IEnumerable<ClaimEntity> Claims { get; init; }

    /// <summary>
    /// Access token
    /// </summary>
    public string AccessToken { get; set; }

    /// <summary>
    /// Refresh token
    /// </summary>
    public string RefreshToken { get; set; }

    /// <summary>
    /// Identity token
    /// </summary>
    public string IdentityToken { get; set; }

    /// <summary>
    /// Access token expiration date
    /// </summary>
    public DateTimeOffset AccessTokenExpiration { get; set; }

    /// <summary>
    /// Type of authentication
    /// </summary>
    public string AuthenticationType { get; init; }

    /// <summary>
    /// Type of role claim
    /// </summary>
    public string RoleClaintType { get; init; }

    /// <summary>
    /// Type of name claim
    /// </summary>
    public string NameClaimType { get; init; }
}