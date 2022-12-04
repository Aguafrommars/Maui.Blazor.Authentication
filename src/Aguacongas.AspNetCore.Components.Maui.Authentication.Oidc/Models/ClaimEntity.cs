namespace Aguacongas.AspNetCore.Components.Maui.Authentication.Oidc.Models;

public record ClaimEntity
{
    public string Type { get; init; }

    public string Value { get; init; }
}