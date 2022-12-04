namespace Aguacongas.AspNetCore.Components.Maui.Authentication.Oidc.Models;

public record AuthenticationEntity
{
    public IEnumerable<ClaimEntity> Claims { get; init; }

    public string AccessToken { get; set; }

    public string RefreshToken { get; set; }

    public string IdentityToken { get; set; }

    public DateTimeOffset AccessTokenExpiration { get; set; }
    public string AuthenticationType { get; init; }
    public string RoleClaintType { get; init; }
    public string NameClaimType { get; init; }
}