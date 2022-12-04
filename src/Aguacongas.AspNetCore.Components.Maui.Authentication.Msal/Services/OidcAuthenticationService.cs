using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Client;
using System.Diagnostics;
using System.Security.Claims;

namespace Aguacongas.AspNetCore.Components.Maui.Authentication.Msal.Services;

public class OidcAuthenticationService<TRemoteAuthenticationState> :
        AuthenticationStateProvider,
        IRemoteAuthenticationService<TRemoteAuthenticationState>,
        IAccessTokenProvider
        where TRemoteAuthenticationState : RemoteAuthenticationState
{
    private readonly IPublicClientApplication _oidcClient;
    private readonly IOptionsSnapshot<RemoteAuthenticationOptions<OidcProviderOptions>> _options;
    private readonly NavigationManager _navigation;
    private ClaimsPrincipal _principal = new();

    public OidcAuthenticationService(IPublicClientApplication oidcClient,
        NavigationManager navigation,
        IOptionsSnapshot<RemoteAuthenticationOptions<OidcProviderOptions>> options)
    {
        _oidcClient = oidcClient;
        _navigation = navigation;
        _options = options;
    }

    public Task<RemoteAuthenticationResult<TRemoteAuthenticationState>> CompleteSignInAsync(RemoteAuthenticationContext<TRemoteAuthenticationState> context)
    => throw new NotImplementedException();

    public Task<RemoteAuthenticationResult<TRemoteAuthenticationState>> CompleteSignOutAsync(RemoteAuthenticationContext<TRemoteAuthenticationState> context)
    => throw new NotImplementedException();

    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    => Task.FromResult(new AuthenticationState(_principal));    

    public ValueTask<AccessTokenResult> RequestAccessToken()
    => RequestAccessToken(new AccessTokenRequestOptions
    {
        Scopes = _options.Value.ProviderOptions.DefaultScopes
    });

    public async ValueTask<AccessTokenResult> RequestAccessToken(AccessTokenRequestOptions options)
    {
        AuthenticationResult result = null;
        bool tryInteractiveLogin = false;

        var accounts = await _oidcClient.GetAccountsAsync().ConfigureAwait(false);

        try
        {
            result = await _oidcClient
                .AcquireTokenSilent(options.Scopes, accounts.FirstOrDefault())
                .ExecuteAsync();
        }
        catch (MsalUiRequiredException)
        {
            tryInteractiveLogin = true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"MSAL Silent Error: {ex.Message}");
        }

        if (tryInteractiveLogin)
        {
            try
            {
                result = await _oidcClient
                    .AcquireTokenInteractive(options.Scopes)
                    .ExecuteAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"MSAL Interactive Error: {ex.Message}");
            }
        }

        return new AccessTokenResult(result is not null ? AccessTokenResultStatus.Success : AccessTokenResultStatus.RequiresRedirect,
            new AccessToken
            {
                Expires = result?.ExpiresOn ?? DateTimeOffset.MinValue,
                Value = result?.AccessToken,
                GrantedScopes = options.Scopes.ToArray()
            },
            result is null ? _options.Value.AuthenticationPaths.LogInPath : null,
            result is null ? new InteractiveRequestOptions
            {
                Interaction = InteractionType.GetToken,
                ReturnUrl = GetReturnUrl(options.ReturnUrl)
            } : null);
    }

    public async Task<RemoteAuthenticationResult<TRemoteAuthenticationState>> SignInAsync(RemoteAuthenticationContext<TRemoteAuthenticationState> context)
    {
        try
        {
            var result = await _oidcClient.AcquireTokenInteractive(_options.Value.ProviderOptions.DefaultScopes)
                .ExecuteAsync()
                .ConfigureAwait(false);

            _principal = result.ClaimsPrincipal;
            UpdateUser(Task.FromResult(result.ClaimsPrincipal));
            
            return new RemoteAuthenticationResult<TRemoteAuthenticationState>
            {
                State = context.State,
                Status = RemoteAuthenticationStatus.Success
            };
        }
        catch(Exception e)
        {
            _principal = new ClaimsPrincipal();
            UpdateUser(Task.FromResult(_principal));

            return new RemoteAuthenticationResult<TRemoteAuthenticationState>
            {
                ErrorMessage = e.Message,
                State = context.State,
                Status = RemoteAuthenticationStatus.Failure
            };
        }
    }

    public async Task<RemoteAuthenticationResult<TRemoteAuthenticationState>> SignOutAsync(RemoteAuthenticationContext<TRemoteAuthenticationState> context)
    {
        var accounts = await _oidcClient.GetAccountsAsync().ConfigureAwait(false);
        foreach(var account in accounts)
        {
            await _oidcClient.RemoveAsync(account).ConfigureAwait(false);
        }

        _principal = new ClaimsPrincipal();

        UpdateUser(Task.FromResult(_principal));

        return new RemoteAuthenticationResult<TRemoteAuthenticationState>
        {
            State = context.State,
            Status = RemoteAuthenticationStatus.Success
        };
    }

    private void UpdateUser(Task<ClaimsPrincipal> task)
    {
        NotifyAuthenticationStateChanged(UpdateAuthenticationState(task));

        static async Task<AuthenticationState> UpdateAuthenticationState(Task<ClaimsPrincipal> futureUser) => new AuthenticationState(await futureUser);
    }

    private string GetReturnUrl(string customReturnUrl)
    {
        try
        {
            return customReturnUrl != null ? _navigation.ToAbsoluteUri(customReturnUrl).AbsoluteUri : _navigation.Uri;
        }
        catch
        {
            return customReturnUrl;
        }
    }
}