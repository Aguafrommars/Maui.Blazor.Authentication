# Aguacongas.AspNetCore.Components.Maui.Authentication.Oidc

Simplify OIDC authentication for MAUI Blazor app

## Use

Install the package in your MAUI Blazor project.

```bash
dotnet add package Aguacongas.AspNetCore.Components.Maui.Authentication.Oidc
```

Update your `MauiProgram` to setup the DI

```c#
string authorityUrl =
    DeviceInfo.Platform == DevicePlatform.Android ? "https://10.0.2.2:5001" : "https://localhost:5001";

builder.Services.AddMauiOidcAuthentication(options =>
{
  var providerOptions = options.ProviderOptions;
  providerOptions.Authority = authorityUrl;
  providerOptions.ClientId = "mauiblazorsample";
  providerOptions.RedirectUri = "mauiblazorsample://authentication/login-callback";
  providerOptions.PostLogoutRedirectUri = "mauiblazorsample://authentication/logout-callback";
  providerOptions.DefaultScopes.Add("offline_access");
  providerOptions.DefaultScopes.Add("scope1");
}, ConfigureHttpMessgeBuilder);
```

`AddMauiOidcAuthentication` adds [`AuthenticationStateProvider service`](https://learn.microsoft.com/en-us/aspnet/core/blazor/security/#authenticationstateprovider-service) in DI so you can use [`AuthenticationState, CascadingAuthenticationState`](https://learn.microsoft.com/en-us/aspnet/core/blazor/security/#expose-the-authentication-state-as-a-cascading-parameter) and [`AuthorizationMessageHandler`](https://learn.microsoft.com/en-us/aspnet/core/blazor/security/webassembly/additional-scenarios#attach-tokens-to-outgoing-requests)

## Platform Specific Configurations

We need to declare the application uri scheme for each platforms. In following samples, the scheme is **mauiblazorsample**.

### Android

Update *AndroidManifest.xml* by adding the queries section :

```xml
<?xml version="1.0" encoding="utf-8"?>
<manifest xmlns:android="http://schemas.android.com/apk/res/android">
  ...
  <queries>
    <intent>
      <action android:name="android.support.customtabs.action.CustomTabsService" />
    </intent>
  </queries>
</manifest>
```

Add a `OidcAuthenticationCallbackActivity` class deriving from `WebAuthenticatorCallbackActivity` declaring the application uri scheme:

```cs
using Android.App;
using Android.Content;
using Android.Content.PM;

namespace Maui.Blazor.Client.Platforms.Android;

[Activity(NoHistory = true, LaunchMode = LaunchMode.SingleTop, Exported = true)]
[IntentFilter(new[] { Intent.ActionView },
          Categories = new[] {
            Intent.CategoryDefault,
            Intent.CategoryBrowsable
          },
          DataScheme = CALLBACK_SCHEME,
          DataPaths = new[] {
              "authentication/login-callback",
              "authentication/login-callback"
              })]
public class OidcAuthenticationCallbackActivity : WebAuthenticatorCallbackActivity
{
    const string CALLBACK_SCHEME = "mauiblazorsample";
}
```

### IOS and MacCatalyst

Update *Info.plist* file. 

```xml
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
  <dict>
    ...
    <key>CFBundleURLTypes</key>
    <array>
      <dict>
        <key>CFBundleURLName</key>
        <string>mauiblazorsample</string>
        <key>CFBundleURLSchemes</key>
        <array>
          <string>mauiblazorsample</string>
        </array>
        <key>CFBundleTypeRole</key>
        <string>Editor</string>
      </dict>
    </array>
  </dict>
</plist>
```

Update *AppDelegate.cs* to override `OpenUrl` and `ContinueUserActivity` methods

```cs
using Foundation;
using UIKit;

namespace Maui.Blazor.Client;

[Register("AppDelegate")]
public class AppDelegate : MauiUIApplicationDelegate
{
	protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();

    public override bool OpenUrl(UIApplication app, NSUrl url, NSDictionary options)
    {
        if (Platform.OpenUrl(app, url, options))
        {
            return true;
        }

        return base.OpenUrl(app, url, options);
    }

    public override bool ContinueUserActivity(UIApplication application, NSUserActivity userActivity, UIApplicationRestorationHandler completionHandler)
    {
        if (Platform.ContinueUserActivity(application, userActivity, completionHandler))
        {
            return true;
        }

        return base.ContinueUserActivity(application, userActivity, completionHandler);
    }
}
```

#### iOS Keychain Entitlement

Tokens are  stored using default `SecureStorage` and in iOS you need to add the [Keychain Entitlement](https://learn.microsoft.com/en-us/dotnet/maui/ios/entitlements?view=net-maui-7.0&tabs=vs#keychain).

Add a *Entitlements.plist* in *Platforms/iOS* :

```xml
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
  <dict>
    <key>keychain-access-groups</key>
    <array>
      <string>$(AppIdentifierPrefix)client.maui.blazor</string>            
    </array>
  </dict>
</plist>
```

The sting suffix (**client.maui.blazor** in this sample) should match the Bundle Identifier of *info.plist* file.

```xml
<key>CFBundleIdentifier</key>
<string>client.maui.blazor</string>
```

### Windows

Add following protocol extension in *Package.appxmanifest* file.

```xml
<?xml version="1.0" encoding="utf-8"?>
<Package
  xmlns="http://schemas.microsoft.com/appx/manifest/foundation/windows10"
  xmlns:uap="http://schemas.microsoft.com/appx/manifest/uap/windows10"
  xmlns:mp="http://schemas.microsoft.com/appx/2014/phone/manifest"
  xmlns:rescap="http://schemas.microsoft.com/appx/manifest/foundation/windows10/restrictedcapabilities"
  IgnorableNamespaces="uap rescap">

    ...
    
	<Applications>
		<Application Id="App" Executable="$targetnametoken$.exe" EntryPoint="$targetentrypoint$">
			...
			<Extensions>
				<uap:Extension Category="windows.protocol">
                    <uap:Protocol Name="mauiblazorsample">
                        <uap:DisplayName>MAUI blazor sample</uap:DisplayName>
                    </uap:Protocol>
				</uap:Extension>
			</Extensions>

		</Application>
	</Applications>

	...

</Package>
```

## Local development

To configure the internal `HttpMessageHandler` to trust self signed certificate or local url you can provide a configuration method like this one:

```c#
private static void ConfigureHttpMessgeBuilder(HttpMessageHandlerBuilder builder)
{
#if IOS
    var handler = new NSUrlSessionHandler();
    handler.TrustOverrideForUrl = (sender, url, trust) =>
    {
        if (url.StartsWith("https://localhost:5001"))
        {
            return true;
        }
        return false;
    };
builder.PrimaryHandler = handler;
#else
var handler = builder.PrimaryHandler as HttpClientHandler;
    handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) =>
    {
        if (cert != null && cert.Issuer.Equals("CN=localhost"))
        {
            return true;
        }
        return errors == System.Net.Security.SslPolicyErrors.None;
    };
#endif
}
```

## Sample

The github repository contains a [sample](https://github.com/Aguafrommars/Maui.Blazor.Authentication/tree/main/samples) containing:

* a *Blazor WASM* project
* a *MAUI Blazor* project
* a Blazor UI project shared by *Blazor WASM* and *MAUI Blazor*
* an OIDC server project containing the WeatherForecast API


