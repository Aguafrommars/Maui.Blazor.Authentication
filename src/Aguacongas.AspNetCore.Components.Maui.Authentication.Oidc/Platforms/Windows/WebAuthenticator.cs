using Microsoft.Windows.AppLifecycle;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Web;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using AppLifecycle = Microsoft.Windows.AppLifecycle;

namespace WinUIEx;

/// <summary>
/// Handles OAuth redirection to the system browser and re-activation.
/// </summary>
/// <remarks>
/// <para>
/// Your app must be configured for OAuth. In you app package's <c>Package.appxmanifest</c> under Declarations, add a 
/// Protocol declaration and add the scheme you registered for your application's oauth redirect url under "Name".
/// </para>
/// </remarks>
internal sealed class WebAuthenticator: IWebAuthenticator
{
    /// <summary>
    /// Singleton instance of <see cref="WebAuthenticator"/>
    /// </summary>
    public static readonly WebAuthenticator Instance = new();

    private readonly Dictionary<string, TaskCompletionSource<Uri>> tasks = new();
    private readonly AppLifecycle.AppInstance _appInstance;
    private readonly Package _package;

    private WebAuthenticator()
    {
        _appInstance = AppLifecycle.AppInstance.GetCurrent() ?? throw new InvalidOperationException("The WebAuthenticator requires an app instance");
        _package = Package.Current ?? throw new InvalidOperationException("The WebAuthenticator requires a packaged app with an AppxManifest");
        SubcribeToActivated(_appInstance);
    }

    /// <summary>
    /// Anthenticates the user
    /// </summary>
    /// <param name="webAuthenticatorOptions">The authentication options</param>
    /// <returns></returns>
    public Task<WebAuthenticatorResult> AuthenticateAsync(WebAuthenticatorOptions webAuthenticatorOptions)
    => AuthenticateAsync(webAuthenticatorOptions.Url, webAuthenticatorOptions.CallbackUrl);


    [ModuleInitializer]
    [SuppressMessage("Usage", "CA2255:The 'ModuleInitializer' attribute should not be used in libraries", Justification = "<Pending>")]
    internal static void Init()
    {
        try
        {
            Instance.OnAppCreation();
        }
        catch (Exception ex)
        {
            Trace.WriteLine($"WinUIEx: Failed to initialize the WebAuthenticator: {ex.Message}", "WinUIEx");
        }
    }

    /// <summary>
    /// Method call on application initialization.
    /// </summary>
    public void OnAppCreation()
    {
        var activatedEventArgs = _appInstance?.GetActivatedEventArgs();
        if (activatedEventArgs is null)
        {
            return;
        }
            
        var state = GetState(activatedEventArgs);
        if (state["appInstanceId"] is string id && state["signinId"] is string signinId && !string.IsNullOrEmpty(signinId))
        {
            var instance = AppLifecycle.AppInstance.GetInstances().FirstOrDefault(i => i.Key == id);

            if (instance is not null && !instance.IsCurrent)
            {
                // Redirect to correct instance and close this one
                instance.RedirectActivationToAsync(activatedEventArgs).AsTask().Wait();
                Process.GetCurrentProcess().Kill();
            }
        }
        else
        {
            if (string.IsNullOrEmpty(_appInstance.Key))
            {
                AppLifecycle.AppInstance.FindOrRegisterForKey(Guid.NewGuid().ToString());
            }
        }
    }

    [SuppressMessage("Minor Code Smell", "S1075:URIs should not be hardcoded", Justification = "Namespaces")]
    private bool IsUriProtocolDeclared(string scheme)
    {
        var docPath = Path.Combine(_package.InstalledLocation.Path, "AppxManifest.xml");
        var doc = XDocument.Load(docPath, LoadOptions.None);
        var reader = doc.CreateReader();
        var namespaceManager = new XmlNamespaceManager(reader.NameTable);
        namespaceManager.AddNamespace("x", "http://schemas.microsoft.com/appx/manifest/foundation/windows10");
        namespaceManager.AddNamespace("uap", "http://schemas.microsoft.com/appx/manifest/uap/windows10");

        // Check if the protocol was declared
        var decl = doc.Root?.XPathSelectElements($"//uap:Extension[@Category='windows.protocol']/uap:Protocol[@Name='{scheme}']", namespaceManager);

        return decl?.Any() == true;
    }

    private static NameValueCollection GetState(AppActivationArguments activatedEventArgs)
    {
        if (activatedEventArgs.Kind == ExtendedActivationKind.Protocol &&
            activatedEventArgs.Data is IProtocolActivatedEventArgs protocolArgs)
        {
            return GetState(protocolArgs);
        }
        return new NameValueCollection(0);
    }

    private static NameValueCollection GetState(IProtocolActivatedEventArgs protocolArgs)
    {
        var query = protocolArgs.Uri.Query;
        var vals = !string.IsNullOrEmpty(query) ? HttpUtility.ParseQueryString(query) : null;

        if (vals?["state"] is null)
        {
            var fragment = protocolArgs.Uri.Fragment;
            if (fragment.StartsWith("#"))
            {
                fragment = fragment[1..];
            }
            vals = HttpUtility.ParseQueryString(fragment);
        }

        if (vals?["state"] is string state)
        {
            var vals2 = HttpUtility.ParseQueryString(state);
            // Some services doesn't like & encoded state parameters, and breaks them out separately.
            // In that case copy over the important values
            if (vals.AllKeys.Contains("appInstanceId") && !vals2.AllKeys.Contains("appInstanceId"))
            {
                vals2.Add("appInstanceId", vals["appInstanceId"]);
            }
            if (vals.AllKeys.Contains("signinId") && !vals2.AllKeys.Contains("signinId"))
            {
                vals2.Add("signinId", vals["signinId"]);
            }

            return vals2;
        }
        return new NameValueCollection(0);
    }

    private void SubcribeToActivated(AppLifecycle.AppInstance appInstance)
    {
        appInstance.Activated += CurrentAppInstance_Activated;
    }

    private void CurrentAppInstance_Activated(object sender, AppActivationArguments e)
    {
        if (e.Kind == ExtendedActivationKind.Protocol &&
            e.Data is IProtocolActivatedEventArgs protocolArgs)
        {
            var vals = GetState(protocolArgs);
            if (vals["signinId"] is string signinId)
            {
                ResumeSignin(protocolArgs.Uri, signinId);
            }
        }
    }

    private void ResumeSignin(Uri callbackUri, string signinId)
    {
        if (signinId != null && tasks.TryGetValue(signinId, out var task))
        {
            tasks.Remove(signinId);
            task.TrySetResult(callbackUri);
        }
    }

    private async Task<WebAuthenticatorResult> AuthenticateAsync(Uri authorizeUri, Uri callbackUri)
    {
        if (!IsUriProtocolDeclared(callbackUri.Scheme))
        {
            throw new InvalidOperationException($"The URI Scheme {callbackUri.Scheme} is not declared in AppxManifest.xml");
        }
        var signinId = Guid.NewGuid();
        var uriBuilded = new UriBuilder(authorizeUri);

        var query = HttpUtility.ParseQueryString(authorizeUri.Query);
        var state = $"appInstanceId={_appInstance.Key}&signinId={signinId}";
        if (query["state"] is string oldstate && !string.IsNullOrEmpty(oldstate))
        {
            // Encode the state parameter
            state += $"&state={HttpUtility.UrlEncode(oldstate)}";
        }
        query["state"] = state;
        uriBuilded.Query = query.ToString();
        authorizeUri = uriBuilded.Uri;

        var tcs = new TaskCompletionSource<Uri>();
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "rundll32.exe",
                Arguments = $"url.dll,FileProtocolHandler {authorizeUri}",
                UseShellExecute = true
            }
        };
        process.Start();
        tasks.Add(signinId.ToString(), tcs);
        var uri = await tcs.Task.ConfigureAwait(false);
        return new WebAuthenticatorResult(uri);
    }
}