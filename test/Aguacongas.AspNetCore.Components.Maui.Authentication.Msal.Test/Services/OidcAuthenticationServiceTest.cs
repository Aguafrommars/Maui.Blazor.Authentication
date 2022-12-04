using Aguacongas.AspNetCore.Components.Maui.Authentication.Msal.Services;
using Aguacongas.AspNetCore.Components.Maui.Authentication.Test.Utils;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Client;
using Moq;
using System.Net;

namespace Aguacongas.AspNetCore.Components.Maui.Authentication.Msal.Test.Services;

public class OidcAuthenticationServiceTest
{
    [Fact]
    public async Task CompleteSignInAsync_should_not_be_implemented()
    {
        var sut = new OidcAuthenticationService<RemoteAuthenticationState>(null, null, null);
        await Assert.ThrowsAsync<NotImplementedException>(() => sut.CompleteSignInAsync(null));
    }

    [Fact]
    public async Task CompleteSignOutAsync_should_not_be_implemented()
    {
        var sut = new OidcAuthenticationService<RemoteAuthenticationState>(null, null, null);
        await Assert.ThrowsAsync<NotImplementedException>(() => sut.CompleteSignOutAsync(null));
    }

    [Fact]
    public async Task GetAuthenticationStateAsync_current_principa()
    {
        var sut = new OidcAuthenticationService<RemoteAuthenticationState>(null, null, null);

        var result = await sut.GetAuthenticationStateAsync();
        Assert.NotNull(result.User);
    }

    [Fact]
    public async Task RequestAccessToken_should_refresh_token()
    {
        var oidcClient = PublicClientApplicationBuilder
            .Create("test")
            .WithAuthority("https://exemple.com")
            .WithHttpClientFactory(new MsalHttpClientFactory(new HttpClient(new FakeHttpMessageHandler
            {
                Func = r =>
                {
                    return (r.RequestUri?.ToString()) switch
                    {
                        "https://exemple.com/.well-known/openid-configuration" => Task.FromResult(new HttpResponseMessage
                        {
                            Content = new StringContent(File.ReadAllText("discovery.json"))
                        }),
                        "https://exemple.com/.well-known/openid-configuration/jwks" => Task.FromResult(new HttpResponseMessage
                        {
                            Content = new StringContent(File.ReadAllText("jwks.json"))
                        }),
                        "https://exemple.com/connect/token" => Task.FromResult(new HttpResponseMessage
                        {
                            Content = new StringContent(File.ReadAllText("expired-token.json"))
                        }),
                        "https://exemple.com/connect/userinfo" => Task.FromResult(new HttpResponseMessage
                        {
                            Content = new StringContent(File.ReadAllText("userinfo.json"))
                        }),
                        _ => throw new InvalidOperationException($"Uri unknow {r.RequestUri}"),
                    };
                }
            })))
            .WithRedirectUri("test://authentication/login-callback")
            .Build();

        var snapshotMock = new Mock<IOptionsSnapshot<RemoteAuthenticationOptions<OidcProviderOptions>>>();
        snapshotMock.Setup(m => m.Get(It.IsAny<string>())).Returns(new RemoteAuthenticationOptions<OidcProviderOptions>
        {

        });
        var sut = new OidcAuthenticationService<RemoteAuthenticationState>(oidcClient, new FakeNavigationManager(), snapshotMock.Object);
        await sut.SignInAsync(new RemoteAuthenticationContext<RemoteAuthenticationState>());

        var result = await sut.RequestAccessToken();
        Assert.Equal(AccessTokenResultStatus.Success, result.Status);
    }

    [Fact]
    public async Task RequestAccessToken_should_get_token_for_a_different_scope()
    {
        var oidcClient = PublicClientApplicationBuilder
            .Create("test")
            .WithAuthority("https://exemple.com")
            .WithHttpClientFactory(new MsalHttpClientFactory(new HttpClient(new FakeHttpMessageHandler
                {
                    Func = r =>
                    {
                        return (r.RequestUri?.ToString()) switch
                        {
                            "https://exemple.com/.well-known/openid-configuration" => Task.FromResult(new HttpResponseMessage
                            {
                                Content = new StringContent(File.ReadAllText("discovery.json"))
                            }),
                            "https://exemple.com/.well-known/openid-configuration/jwks" => Task.FromResult(new HttpResponseMessage
                            {
                                Content = new StringContent(File.ReadAllText("jwks.json"))
                            }),
                            "https://exemple.com/connect/token" => Task.FromResult(new HttpResponseMessage
                            {
                                Content = new StringContent(File.ReadAllText("token.json"))
                            }),
                            "https://exemple.com/connect/userinfo" => Task.FromResult(new HttpResponseMessage
                            {
                                Content = new StringContent(File.ReadAllText("userinfo.json"))
                            }),
                            _ => throw new InvalidOperationException($"Uri unknow {r.RequestUri}"),
                        };
                    }
            })))
            .WithRedirectUri("test://authentication/login-callback")
            .Build();

        var snapshotMock = new Mock<IOptionsSnapshot<RemoteAuthenticationOptions<OidcProviderOptions>>>();
        snapshotMock.Setup(m => m.Get(It.IsAny<string>())).Returns(new RemoteAuthenticationOptions<OidcProviderOptions>
        {

        });
        var sut = new OidcAuthenticationService<RemoteAuthenticationState>(oidcClient, new FakeNavigationManager(), snapshotMock.Object);
        await sut.SignInAsync(new RemoteAuthenticationContext<RemoteAuthenticationState>());

        var result = await sut.RequestAccessToken(new AccessTokenRequestOptions
        {
            Scopes = new[]
            {
                "api"
            }
        });
        Assert.Equal(AccessTokenResultStatus.Success, result.Status);
    }

    [Fact]
    public async Task SignOutAsync_should_signout_user()
    {
        var oidcClient = PublicClientApplicationBuilder
            .Create(Guid.NewGuid().ToString())
            .WithAuthority("https://exemple.com/default")
            .WithHttpClientFactory(new MsalHttpClientFactory(new HttpClient(new FakeHttpMessageHandler
            {
                Func = r =>
                {
                    var requestUri = r.RequestUri;
                    if (requestUri?.LocalPath?.StartsWith("/common") == true)
                    {
                        var response = new HttpResponseMessage
                        {
                            StatusCode = HttpStatusCode.Redirect,
                        };
                        response.Headers.Location = new Uri($"https://exemple.com/{requestUri.PathAndQuery.Substring("/default".Length)}");
                        return Task.FromResult(response);
                    }
                    return (requestUri?.ToString()) switch
                    {
                        "https://exemple.com/.well-known/openid-configuration" => Task.FromResult(new HttpResponseMessage
                        {
                            Content = new StringContent(File.ReadAllText("discovery.json"))
                        }),
                        "https://exemple.com/.well-known/openid-configuration/jwks" => Task.FromResult(new HttpResponseMessage
                        {
                            Content = new StringContent(File.ReadAllText("jwks.json"))
                        }),
                        "https://exemple.com/connect/token" => Task.FromResult(new HttpResponseMessage
                        {
                            Content = new StringContent(File.ReadAllText("token.json"))
                        }),
                        "https://exemple.com/connect/userinfo" => Task.FromResult(new HttpResponseMessage
                        {
                            Content = new StringContent(File.ReadAllText("userinfo.json"))
                        }),
                        _ => throw new InvalidOperationException($"Uri unknow {r.RequestUri}"),
                    };
                }
            })))
            .WithRedirectUri("test://authentication/login-callback")
            .Build();

        var options = new RemoteAuthenticationOptions<OidcProviderOptions>();
        var snapshotMock = new Mock<IOptionsSnapshot<RemoteAuthenticationOptions<OidcProviderOptions>>>();
        snapshotMock.SetupGet(m => m.Value).Returns(options);
        var sut = new OidcAuthenticationService<RemoteAuthenticationState>(oidcClient, new FakeNavigationManager(), snapshotMock.Object);
        await sut.SignInAsync(new RemoteAuthenticationContext<RemoteAuthenticationState>());

        var result = await sut.SignOutAsync(new RemoteAuthenticationContext<RemoteAuthenticationState>());
        Assert.Equal(RemoteAuthenticationStatus.Success, result.Status);
    }
}
