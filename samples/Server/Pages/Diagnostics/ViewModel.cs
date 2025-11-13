// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using Duende.IdentityModel;
using Microsoft.AspNetCore.Authentication;
using System.Text;
using System.Text.Json;

namespace OidcAndApiServer.Pages.Diagnostics;

public class ViewModel
{
    public ViewModel(AuthenticateResult result)
    {
        AuthenticateResult = result;

        if (result.Properties.Items.TryGetValue("client_list", out string encoded))
        {
            var bytes = Base64Url.Decode(encoded);
            var value = Encoding.UTF8.GetString(bytes);

            Clients = JsonSerializer.Deserialize<string[]>(value);
        }
    }

    public AuthenticateResult AuthenticateResult { get; }
    public IEnumerable<string> Clients { get; } = [];
}