using Aguacongas.AspNetCore.Components.Maui.Authentication.Oidc.Models;
using Aguacongas.AspNetCore.Components.Maui.Authentication.Oidc.Services;
using Moq;

namespace Aguacongas.AspNetCore.Components.Maui.Authentication.Oidc.Test.Services;

public class AuthenticationStoreTest
{
    [Fact]
    public async Task GetAsync_should_read_value_from_secure_storage()
    {
        var secureStorageMock = new Mock<ISecureStorage>();
        secureStorageMock.Setup(m => m.GetAsync(It.IsAny<string>())).Verifiable();

        var sut = new AuthenticationStore(secureStorageMock.Object);

        var result = await sut.GetAsync(Guid.NewGuid().ToString());

        secureStorageMock.Verify();
    }

    [Fact]
    public async Task SetAsync_should_write_value_to_secure_storage()
    {
        var secureStorageMock = new Mock<ISecureStorage>();
        secureStorageMock.Setup(m => m.SetAsync(It.IsAny<string>(), It.IsAny<string>())).Verifiable();

        var sut = new AuthenticationStore(secureStorageMock.Object);

        await sut.SetAsync(Guid.NewGuid().ToString(), new AuthenticationEntity());

        secureStorageMock.Verify();
    }

    [Fact]
    public void _should_remove_value_from_secure_storage()
    {
        var secureStorageMock = new Mock<ISecureStorage>();
        secureStorageMock.Setup(m => m.Remove(It.IsAny<string>())).Verifiable();

        var sut = new AuthenticationStore(secureStorageMock.Object);

        sut.Delete(Guid.NewGuid().ToString());

        secureStorageMock.Verify();
    }
}
