namespace Aguacongas.AspNetCore.Components.Maui.Authentication.Models;

public record ClaimEntity
{
    public string Type { get; init; }

    public string Value { get; init; }
}