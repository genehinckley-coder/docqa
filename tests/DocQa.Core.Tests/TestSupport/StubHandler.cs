using System.Net;
using System.Text;

namespace DocQa.Core.Tests.TestSupport;

/// <summary>
/// The innermost HTTP handler in a test: records each request and answers it without touching the
/// network.
/// </summary>
/// <param name="respond">Builds the response to the nth request, counting from 1.</param>
internal sealed class StubHandler(Func<int, HttpResponseMessage> respond) : DelegatingHandler
{
    private readonly List<CapturedRequest> _requests = [];

    public IReadOnlyList<CapturedRequest> Requests
    {
        get
        {
            lock (_requests)
            {
                return [.. _requests];
            }
        }
    }

    public static HttpResponseMessage Json(string json, HttpStatusCode status = HttpStatusCode.OK) =>
        new(status) { Content = new StringContent(json, Encoding.UTF8, "application/json") };

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        int number;
        lock (_requests)
        {
            _requests.Add(CapturedRequest.From(request));
            number = _requests.Count;
        }

        HttpResponseMessage response = respond(number);
        response.RequestMessage = request;
        return Task.FromResult(response);
    }
}

/// <summary>What a test needs to know about a request, captured before it is disposed.</summary>
internal sealed record CapturedRequest(
    HttpMethod Method,
    Uri? Uri,
    string? ApiKey,
    bool HasAuthorization,
    IReadOnlyList<(string Name, string Value)> Headers)
{
    public static CapturedRequest From(HttpRequestMessage request) => new(
        request.Method,
        request.RequestUri,
        request.Headers.TryGetValues("x-api-key", out IEnumerable<string>? keys) ? keys.Single() : null,
        request.Headers.Contains("Authorization"),
        [.. request.Headers.SelectMany(header => header.Value.Select(value => (header.Key, value)))]);
}
