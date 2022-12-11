using Aguacongas.AspNetCore.Components.Maui.Authentication.Oidc.Abstraction;
using Aguacongas.AspNetCore.Components.Maui.Authentication.Oidc.Models;
using Aguacongas.AspNetCore.Components.Maui.Authentication.Oidc.Services;
using Aguacongas.AspNetCore.Components.Maui.Authentication.Oidc.Test.Utils;
using Ch.Sien.PwdManagement.Front.Test;
using IdentityModel.Client;
using IdentityModel.OidcClient;
using IdentityModel.OidcClient.Browser;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.Extensions.Options;
using Moq;
using IBrowser = IdentityModel.OidcClient.Browser.IBrowser;

namespace Aguacongas.AspNetCore.Components.Maui.Authentication.Oidc.Test.Services;

public class OidcAuthenticationServiceTest
{
    [Fact]
    public async Task CompleteSignInAsync_should_not_be_implemented()
    {
        var sut = new OidcAuthenticationService<RemoteAuthenticationState>(null, null, null, null);
        await Assert.ThrowsAsync<NotImplementedException>(() => sut.CompleteSignInAsync(null));
    }

    [Fact]
    public async Task CompleteSignOutAsync_should_not_be_implemented()
    {
        var sut = new OidcAuthenticationService<RemoteAuthenticationState>(null, null, null, null);
        await Assert.ThrowsAsync<NotImplementedException>(() => sut.CompleteSignOutAsync(null));
    }

    [Fact]
    public async Task GetAuthenticationStateAsync_should_return_state_from_cache()
    {
        var browserMock = new Mock<IBrowser>();

        browserMock.Setup(m => m.InvokeAsync(It.IsAny<BrowserOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync<BrowserOptions, CancellationToken, IBrowser, BrowserResult>((b, c) => new BrowserResult
            {
                Response = CreateUrl(b),
                ResultType = BrowserResultType.Success
            });

        var storeMock = new Mock<IAuthenticationStore>();
        var oidcClient = new OidcClient(new OidcClientOptions
        {
            Authority = "https://exemple.com",
            Browser = browserMock.Object,
            ClientId = "test",
            Scope = "openid profile offline_access",
            RedirectUri = "test://authentication/login-callback",
            HttpClientFactory = o =>
            {
                return new HttpClient(new FakeHttpMessageHandler
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
                });
            }
        });
        var snapshotMock = new Mock<IOptionsSnapshot<RemoteAuthenticationOptions<OidcProviderOptions>>>();
        snapshotMock.SetupGet(m => m.Value).Returns(new RemoteAuthenticationOptions<OidcProviderOptions>
        {

        });
        var sut = new OidcAuthenticationService<RemoteAuthenticationState>(oidcClient, storeMock.Object, new FakeNavigationManager(), snapshotMock.Object);

        var stateBeforeLogin = await sut.GetAuthenticationStateAsync();
        Assert.Null(stateBeforeLogin.User.Identity);

        var result = await sut.SignInAsync(new RemoteAuthenticationContext<RemoteAuthenticationState>());
        Assert.NotNull(result);

        var stateAfterLogin = await sut.GetAuthenticationStateAsync();
        Assert.NotNull(stateAfterLogin.User.Identity);

        Assert.NotEqual(stateBeforeLogin.User, stateAfterLogin.User);
    }

    [Fact]
    public async Task GetAuthenticationStateAsync_should_refresh_token_when_expired()
    {
        var browserMock = new Mock<IBrowser>();

        browserMock.Setup(m => m.InvokeAsync(It.IsAny<BrowserOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync<BrowserOptions, CancellationToken, IBrowser, BrowserResult>((b, c) => new BrowserResult
            {
                Response = CreateUrl(b),
                ResultType = BrowserResultType.Success
            });

        var storeMock = new Mock<IAuthenticationStore>();
        storeMock.Setup(m => m.GetAsync(It.IsAny<string>())).ReturnsAsync(new AuthenticationEntity
        {
            AccessTokenExpiration = DateTimeOffset.UtcNow.AddMinutes(-10),
            RefreshToken = Guid.NewGuid().ToString(),
            Claims = new[]
            {
                new ClaimEntity
                {
                    Type = "name",
                    Value = "test"
                }
            },
            AuthenticationType = "Bearer",
            NameClaimType = "name",
        });

        var oidcClient = new OidcClient(new OidcClientOptions
        {
            Authority = "https://exemple.com",
            Browser = browserMock.Object,
            ClientId = "test",
            Scope = "openid profile offline_access",
            RedirectUri = "test://authentication/login-callback",
            HttpClientFactory = o =>
            {
                return new HttpClient(new FakeHttpMessageHandler
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
                });
            }
        });
        var snapshotMock = new Mock<IOptionsSnapshot<RemoteAuthenticationOptions<OidcProviderOptions>>>();
        snapshotMock.SetupGet(m => m.Value).Returns(new RemoteAuthenticationOptions<OidcProviderOptions>
        {

        });
        var sut = new OidcAuthenticationService<RemoteAuthenticationState>(oidcClient, storeMock.Object, new FakeNavigationManager(), snapshotMock.Object);
        await sut.SignInAsync(new RemoteAuthenticationContext<RemoteAuthenticationState>());

        var state = await sut.GetAuthenticationStateAsync();
        Assert.NotNull(state.User.Identity);
    }

    [Fact]
    public async Task RequestAccessToken_should_refresh_token()
    {
        var browserMock = new Mock<IBrowser>();

        browserMock.Setup(m => m.InvokeAsync(It.IsAny<BrowserOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync<BrowserOptions, CancellationToken, IBrowser, BrowserResult>((b, c) => new BrowserResult
            {
                Response = CreateUrl(b),
                ResultType = BrowserResultType.Success
            });

        var storeMock = new Mock<IAuthenticationStore>();
        storeMock.Setup(m => m.GetAsync(It.IsAny<string>())).ReturnsAsync(new AuthenticationEntity
        {
            AccessTokenExpiration = DateTimeOffset.UtcNow.AddMinutes(-10),
            RefreshToken = Guid.NewGuid().ToString(),
            Claims = new[]
            {
                new ClaimEntity
                {
                    Type = "name",
                    Value = "test"
                }
            },
            AuthenticationType = "Bearer",
            NameClaimType = "name",
        });

        var oidcClient = new OidcClient(new OidcClientOptions
        {
            Authority = "https://exemple.com",
            Browser = browserMock.Object,
            ClientId = "test",
            Scope = "openid profile offline_access",
            RedirectUri = "test://authentication/login-callback",
            HttpClientFactory = o =>
            {
                return new HttpClient(new FakeHttpMessageHandler
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
                });
            }
        });
        var snapshotMock = new Mock<IOptionsSnapshot<RemoteAuthenticationOptions<OidcProviderOptions>>>();
        snapshotMock.SetupGet(m => m.Value).Returns(new RemoteAuthenticationOptions<OidcProviderOptions>
        {
        });
        var sut = new OidcAuthenticationService<RemoteAuthenticationState>(oidcClient, storeMock.Object, new FakeNavigationManager(), snapshotMock.Object);
        await sut.SignInAsync(new RemoteAuthenticationContext<RemoteAuthenticationState>());

        var result = await sut.RequestAccessToken();
        Assert.Equal(AccessTokenResultStatus.Success, result.Status);
    }

    [Fact]
    public async Task RequestAccessToken_should_get_token_for_a_different_scope()
    {
        var browserMock = new Mock<IBrowser>();

        browserMock.Setup(m => m.InvokeAsync(It.IsAny<BrowserOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync<BrowserOptions, CancellationToken, IBrowser, BrowserResult>((b, c) => new BrowserResult
            {
                Response = CreateUrl(b),
                ResultType = BrowserResultType.Success
            });

        var storeMock = new Mock<IAuthenticationStore>();
        var oidcClient = new OidcClient(new OidcClientOptions
        {
            Authority = "https://exemple.com",
            Browser = browserMock.Object,
            ClientId = "test",
            Scope = "openid profile offline_access",
            RedirectUri = "test://authentication/login-callback",
            HttpClientFactory = o =>
            {
                return new HttpClient(new FakeHttpMessageHandler
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
                });
            }
        });
        var snapshotMock = new Mock<IOptionsSnapshot<RemoteAuthenticationOptions<OidcProviderOptions>>>();
        snapshotMock.SetupGet(m => m.Value).Returns(new RemoteAuthenticationOptions<OidcProviderOptions>
        {

        });
        var sut = new OidcAuthenticationService<RemoteAuthenticationState>(oidcClient, storeMock.Object, new FakeNavigationManager(), snapshotMock.Object);
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
        var browserMock = new Mock<IBrowser>();

        browserMock.Setup(m => m.InvokeAsync(It.IsAny<BrowserOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync<BrowserOptions, CancellationToken, IBrowser, BrowserResult>((b, c) => new BrowserResult
            {
                Response = CreateUrl(b),
                ResultType = BrowserResultType.Success
            });

        var storeMock = new Mock<IAuthenticationStore>();
        var oidcClient = new OidcClient(new OidcClientOptions
        {
            Authority = "https://exemple.com",
            Browser = browserMock.Object,
            ClientId = "test",
            Scope = "openid profile offline_access",
            RedirectUri = "test://authentication/login-callback",
            HttpClientFactory = o =>
            {
                return new HttpClient(new FakeHttpMessageHandler
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
                });
            }
        });
        var snapshotMock = new Mock<IOptionsSnapshot<RemoteAuthenticationOptions<OidcProviderOptions>>>();
        snapshotMock.SetupGet(m => m.Value).Returns(new RemoteAuthenticationOptions<OidcProviderOptions>
        {

        });
        var sut = new OidcAuthenticationService<RemoteAuthenticationState>(oidcClient, storeMock.Object, new FakeNavigationManager(), snapshotMock.Object);
        await sut.SignInAsync(new RemoteAuthenticationContext<RemoteAuthenticationState>());

        var result = await sut.SignOutAsync(new RemoteAuthenticationContext<RemoteAuthenticationState>());
        Assert.Equal(RemoteAuthenticationStatus.Success, result.Status);
    }

    private static string CreateUrl(BrowserOptions browserOptions)
    {
        var uri = new Uri(browserOptions.StartUrl);
        var segments = uri.Query.Split('&').Select(s => s.Split('='));
        var state = segments.FirstOrDefault(s => s[0] == "state");
        return new RequestUrl("test://authentication/login-callback")
            .Create(new Parameters(new Dictionary<string, string?>
            {
                ["code"] = Guid.NewGuid().ToString(),
                ["scope"] = "openid profile",
                ["state"] = state?[1],
                ["session_state"] = "F8Qy-cRhrMwSZUVFel4_naUkx6mHilfp0JoU0mtWdto.4D291D31B765485F0FC7151A09211010",
                ["iss"] = "https://exemple.com"
            }));
    }
}
