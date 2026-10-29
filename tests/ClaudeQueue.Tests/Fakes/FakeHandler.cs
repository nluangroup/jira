using System.Net;
using System.Text;

namespace ClaudeQueue.Tests.Fakes;

/// <summary>Queues canned HTTP responses and records the requests and bodies it receives.</summary>
public sealed class FakeHandler : HttpMessageHandler
{
    private readonly Queue<(HttpStatusCode Status, string Body)> responses = new();

    /// <summary>Requests received, in order.</summary>
    public List<HttpRequestMessage> Requests { get; } = [];

    /// <summary>Request bodies received, in order (empty string when none).</summary>
    public List<string> Bodies { get; } = [];

    /// <summary>Queues a response for the next request.</summary>
    public FakeHandler Respond(HttpStatusCode status, string body)
    {
        responses.Enqueue((status, body));
        return this;
    }

    /// <inheritdoc />
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Requests.Add(request);
        Bodies.Add(request.Content is null ? "" : await request.Content.ReadAsStringAsync(cancellationToken));
        var (status, body) = responses.Dequeue();
        return new HttpResponseMessage(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };
    }
}
