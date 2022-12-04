using Aguacongas.AspNetCore.Components.Maui.Authentication.Oidc.Services;
using IdentityModel.OidcClient.Browser;
using Microsoft.Maui.Authentication;
using Moq;

namespace Aguacongas.AspNetCore.Components.Maui.Authentication.Oidc.Test.Services;

public class WebBrowserAuthenticatorTest
{
    [Fact]
    public async Task InvokeAsync_should_return_success_result()
    {
        var authenticatorMock = new Mock<IWebAuthenticator>();
        authenticatorMock.Setup(m => m.AuthenticateAsync(It.IsAny<WebAuthenticatorOptions>()))
            .ReturnsAsync(new WebAuthenticatorResult());

        var sut = new WebBrowserAuthenticator(authenticatorMock.Object);

        var result = await sut.InvokeAsync(new BrowserOptions("https://exemple.com", "https://exemple.com"));

        Assert.Equal(BrowserResultType.Success, result.ResultType);
    }

    [Fact]
    public async Task InvokeAsync_should_return_user_cancel_result_on_task_canceled()
    {
        var authenticatorMock = new Mock<IWebAuthenticator>();
        authenticatorMock.Setup(m => m.AuthenticateAsync(It.IsAny<WebAuthenticatorOptions>()))
            .ThrowsAsync(new TaskCanceledException());

        var sut = new WebBrowserAuthenticator(authenticatorMock.Object);

        var result = await sut.InvokeAsync(new BrowserOptions("https://exemple.com", "https://exemple.com"));

        Assert.Equal(BrowserResultType.UserCancel, result.ResultType);
    }
}
