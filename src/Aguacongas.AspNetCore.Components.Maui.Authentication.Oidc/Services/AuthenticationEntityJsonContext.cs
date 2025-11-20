using Aguacongas.AspNetCore.Components.Maui.Authentication.Oidc.Models;
using System.Text.Json.Serialization;

namespace Aguacongas.AspNetCore.Components.Maui.Authentication.Oidc.Services;

/// <summary>
/// Json serialization context for <see cref="AuthenticationEntity"/>
/// </summary>
[JsonSerializable(typeof(AuthenticationEntity))]
public partial class AuthenticationEntityJsonContext : JsonSerializerContext
{
}
