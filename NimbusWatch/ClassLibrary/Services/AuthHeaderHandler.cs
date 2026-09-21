namespace ClassLibrary.Services
{
  public sealed class AuthHeaderHandler : DelegatingHandler
  {
    private readonly ITokenProvider _tokenProvider;

    public AuthHeaderHandler(ITokenProvider tokenProvider)
    {
      _tokenProvider = tokenProvider;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
      var token = await _tokenProvider.GetTokenAsync(cancellationToken);
      request.Headers.Authorization =
          new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

      return await base.SendAsync(request, cancellationToken);
    }
  }
}
