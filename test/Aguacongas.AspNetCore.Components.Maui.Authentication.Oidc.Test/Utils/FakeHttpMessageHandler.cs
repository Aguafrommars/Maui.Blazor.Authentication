namespace Ch.Sien.PwdManagement.Front.Test;

internal class FakeHttpMessageHandler : HttpMessageHandler
{
    public Func<HttpRequestMessage, Task<HttpResponseMessage>>? Func { get; set; }
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    => Func != null ? Func(request) : throw new InvalidOperationException("Func must be initialized");
}