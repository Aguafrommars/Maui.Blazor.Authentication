using Aguacongas.AspNetCore.Components.Maui.Authentication.Oidc.Abstraction;
using Aguacongas.AspNetCore.Components.Maui.Authentication.Oidc.Models;
using IdentityModel.Client;
using IdentityModel.OidcClient;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.Extensions.Options;
using System.Security.Claims;

namespace Aguacongas.AspNetCore.Components.Maui.Authentication.Oidc.Services;

/// <summary>
/// OIDC authentication service
/// </summary>
/// <typeparam name="TRemoteAuthenticationState"></typeparam>
public class OidcAuthenticationService<TRemoteAuthenticationState> :
        AuthenticationStateProvider,
        IRemoteAuthenticationService<TRemoteAuthenticationState>,
        IAccessTokenProvider
        where TRemoteAuthenticationState : RemoteAuthenticationState
{
    private readonly OidcClient _oidcClient;
    private readonly IAuthenticationStore _store;
    private readonly IOptionsSnapshot<RemoteAuthenticationOptions<OidcProviderOptions>> _options;
    private readonly NavigationManager _navigation;
    private readonly Dictionary<string, AuthenticationEntity> _authenticationCache = new();
    private ClaimsPrincipal _principal = new();

    private DateTimeOffset ExpireAt
    {
        get
        {
            if (_authenticationCache.TryGetValue(_oidcClient.Options.Scope, out AuthenticationEntity entity))
            {
                return entity.AccessTokenExpiration;
            }

            return DateTimeOffset.MinValue;
        }
    }

    /// <summary>
    /// Initialize a new instance of <see cref="OidcAuthenticationService{TRemoteAuthenticationState}"/>
    /// </summary>
    /// <param name="oidcClient">An <see cref="OidcClient"/></param>
    /// <param name="store">A <see cref="IAuthenticationStore"/></param>
    /// <param name="navigation">A <see cref="NavigationManager"/></param>
    /// <param name="options"><see cref="OidcProviderOptions"/></param>
    public OidcAuthenticationService(OidcClient oidcClient,
        IAuthenticationStore store,
        NavigationManager navigation,
        IOptionsSnapshot<RemoteAuthenticationOptions<OidcProviderOptions>> options)
    {
        _oidcClient = oidcClient;
        _store = store;
        _navigation = navigation;
        _options = options;
    }

    /// <inheritdoc />
    public Task<RemoteAuthenticationResult<TRemoteAuthenticationState>> CompleteSignInAsync(RemoteAuthenticationContext<TRemoteAuthenticationState> context)
    => throw new NotImplementedException();

    /// <inheritdoc />
    public Task<RemoteAuthenticationResult<TRemoteAuthenticationState>> CompleteSignOutAsync(RemoteAuthenticationContext<TRemoteAuthenticationState> context)
    => throw new NotImplementedException();

    /// <inheritdoc />
    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        if (ExpireAt < DateTimeOffset.Now.AddMinutes(5))
        {
            _principal = new ClaimsPrincipal();

            var authentication = await GetAuthenticationAsync(_oidcClient.Options.Scope).ConfigureAwait(false);

            if (authentication is not null)
            {
                _principal = new ClaimsPrincipal(
                    new ClaimsIdentity(authentication.Claims.Select(c => new Claim(c.Type, c.Value)),
                    authentication.AuthenticationType,
                    authentication.NameClaimType,
                    authentication.RoleClaintType));
            }
        }

        return new AuthenticationState(_principal);
    }

    /// <inheritdoc />
    public ValueTask<AccessTokenResult> RequestAccessToken()
    => RequestAccessToken(new AccessTokenRequestOptions
    {
        Scopes = _options.Value.ProviderOptions.DefaultScopes
    });

    /// <inheritdoc />
    public async ValueTask<AccessTokenResult> RequestAccessToken(AccessTokenRequestOptions options)
    {
        var authentication = await GetAuthenticationAsync(string.Join(' ', options.Scopes)).ConfigureAwait(false);

        return new AccessTokenResult(authentication is not null ? AccessTokenResultStatus.Success : AccessTokenResultStatus.RequiresRedirect,
            new AccessToken
            {
                Expires = authentication?.AccessTokenExpiration ?? DateTimeOffset.MinValue,
                Value = authentication?.AccessToken,
                GrantedScopes = options.Scopes.ToArray()
            },
            authentication is null ? _options.Value.AuthenticationPaths.LogInPath : null,
            authentication is null ? new InteractiveRequestOptions
            {
                Interaction = InteractionType.GetToken,
                ReturnUrl = GetReturnUrl(options.ReturnUrl)
            } : null);
    }

    /// <inheritdoc />
    public async Task<RemoteAuthenticationResult<TRemoteAuthenticationState>> SignInAsync(RemoteAuthenticationContext<TRemoteAuthenticationState> context)
    {
        var result = await _oidcClient.LoginAsync().ConfigureAwait(false);
        await StoreLoginResultAsync(result).ConfigureAwait(false);

        UpdateUser(Task.FromResult(result.User ?? new ClaimsPrincipal()));

        return new RemoteAuthenticationResult<TRemoteAuthenticationState>
        {
            ErrorMessage = result.Error,
            State = context.State,
            Status = result.IsError ? RemoteAuthenticationStatus.Failure : RemoteAuthenticationStatus.Success
        };
    }

    /// <inheritdoc />
    public async Task<RemoteAuthenticationResult<TRemoteAuthenticationState>> SignOutAsync(RemoteAuthenticationContext<TRemoteAuthenticationState> context)
    {
        await Task.Factory.StartNew(() => _oidcClient.LogoutAsync());

        foreach (var key in _authenticationCache.Keys)
        {
            _store.Delete(key);
        }
        _principal = new ClaimsPrincipal();
        _authenticationCache.Clear();

        UpdateUser(Task.FromResult(_principal));

        return new RemoteAuthenticationResult<TRemoteAuthenticationState>
        {
            State = context.State,
            Status = RemoteAuthenticationStatus.Success
        };
    }

    private async Task<AuthenticationEntity> StoreLoginResultAsync(LoginResult result)
    {
        if (result.User is null)
        {
            return null;
        }

        var identity = result.User.Identity as ClaimsIdentity;
        var entity = new AuthenticationEntity
        {
            AuthenticationType = identity.AuthenticationType,
            NameClaimType = identity.NameClaimType,
            RoleClaintType = identity.RoleClaimType,
            AccessToken = result.AccessToken,
            AccessTokenExpiration = result.AccessTokenExpiration,
            Claims = result.User.Claims.Select(c => new ClaimEntity
            {
                Type = c.Type,
                Value = c.Value
            }),
            IdentityToken = result.IdentityToken,
            RefreshToken = result.RefreshToken
        };

        var scope = _oidcClient.Options.Scope;
        _authenticationCache[scope] = entity;
        await _store.SetAsync(scope, entity).ConfigureAwait(false);
        _principal = result.User;

        return entity;
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


    private async Task<AuthenticationEntity> RefreshTokenAsync(string scope, AuthenticationEntity entity)
    {
        var defaultEntity = await GetOrAddAuthenticationFromCache(_oidcClient.Options.Scope).ConfigureAwait(false);
        if (string.IsNullOrEmpty(defaultEntity?.RefreshToken))
        {
            return null;
        }

        await _oidcClient.PrepareLoginAsync().ConfigureAwait(false);
        var options = _oidcClient.Options;
        var client = options.HttpClientFactory(options);
        var result = await client.RequestRefreshTokenAsync(new RefreshTokenRequest
        {
            Address = options.ProviderInformation.TokenEndpoint,
            ClientId = options.ClientId,
            ClientSecret = options.ClientSecret,
            ClientAssertion = options.ClientAssertion,
            ClientCredentialStyle = options.TokenClientCredentialStyle,
            RefreshToken = defaultEntity.RefreshToken,
            Scope = scope
        }).ConfigureAwait(false);

        if (!result.IsError)
        {
            entity.AccessToken = result.AccessToken;
            entity.AccessTokenExpiration = DateTimeOffset.Now.AddSeconds(result.ExpiresIn);
            entity.RefreshToken = result.RefreshToken;
            entity.IdentityToken = result.IdentityToken;
            await _store.SetAsync(scope, entity).ConfigureAwait(false);
            _authenticationCache[scope] = entity;

            if (entity != defaultEntity)
            {
                defaultEntity.RefreshToken = entity.RefreshToken;
                await _store.SetAsync(_oidcClient.Options.Scope, defaultEntity).ConfigureAwait(false);
            }

            return entity;
        }

        _authenticationCache.Remove(scope);
        return null;
    }

    private async Task<AuthenticationEntity> GetAuthenticationAsync(string scope)
    {
        var entity = await GetOrAddAuthenticationFromCache(scope).ConfigureAwait(false);

        if (entity is null && scope != _oidcClient.Options.Scope)
        {
            // create a new token for a new scope
            return await CreateNewTokenForNewScopeAsync(scope).ConfigureAwait(false);
        }

        if (entity is not null && entity.AccessTokenExpiration < DateTimeOffset.Now.AddMinutes(5))
        {
            entity = await RefreshTokenAsync(scope, entity).ConfigureAwait(false);
        }

        return entity;
    }

    private async Task<AuthenticationEntity> CreateNewTokenForNewScopeAsync(string scope)
    {
        var entity = await GetOrAddAuthenticationFromCache(_oidcClient.Options.Scope).ConfigureAwait(false);
        if (entity is null)
        {
            return null;
        }

        entity = await RefreshTokenAsync(scope, new AuthenticationEntity
        {
            RefreshToken = entity.RefreshToken,
        }).ConfigureAwait(false);
        
        return entity;
    }

    private async Task<AuthenticationEntity> GetOrAddAuthenticationFromCache(string scope)
    {
        if (!_authenticationCache.TryGetValue(scope, out AuthenticationEntity entity))
        {
            entity = await _store.GetAsync(scope).ConfigureAwait(false);
            if (entity is not null)
            {
                _authenticationCache.Add(scope, entity);
            }            
        }

        return entity;
    }
}