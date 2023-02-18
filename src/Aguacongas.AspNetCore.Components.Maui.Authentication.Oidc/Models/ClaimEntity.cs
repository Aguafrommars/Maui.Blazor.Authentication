namespace Aguacongas.AspNetCore.Components.Maui.Authentication.Oidc.Models;

/// <summary>
/// Define a claim
/// </summary>
public record ClaimEntity
{
    /// <summary>
    /// Claim type
    /// </summary>
    public string Type { get; init; }

    /// <summary>
    /// Claim value
    /// </summary>
    public string Value { get; init; }
}