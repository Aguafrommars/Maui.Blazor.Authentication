# Aguacongas.AspNetCore.Components.Maui.Authentication.Oidc sample

This sample demonstrates how we can share blazor code between a [Blazor WebAssembly](https://learn.microsoft.com/en-us/aspnet/core/blazor/?view=aspnetcore-7.0#blazor-webassembly) application and a [MAUI Blazor](https://learn.microsoft.com/en-us/aspnet/core/blazor/hybrid/tutorials/maui?view=aspnetcore-7.0&pivots=windows) application, and use [Blazor authentication and authorization](https://learn.microsoft.com/en-us/aspnet/core/blazor/security/?view=aspnetcore-7.0) with **Aguacongas.AspNetCore.Components.Maui.Authentication.Oidc**.

It contains:

``` bash
├─ Maui.Blazor.Client: a MAUI Blazor project
├─ Server:             an OIDC server project containing the WeatherForecast API 
├─ Shared:             a class library project containing the model shared by the server and clients
├─ Shared.Blazor.UI:   a Razor components project containing pages and components shared by Blazor WASM and MAUI Blazor
└── Web.Blazor.Client: a Blazor WASM project
```

The application is the default Blazor application generated with `dotnet new` template.

You need to be logged to access to weather forecast, the *FetchData.razor* page is decorated with:

```cs
@attribute [Authorize]
```

You can login with *alice* (pwd: alice) or *bob* (pwd: bob).

The OIDC server is a [Duende IdentityServer](https://duendesoftware.com/products/identityserver) built from `dotnet new` template.
