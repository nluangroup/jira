using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace ClaudeQueue.Jira;

/// <summary>Jira Cloud REST v2 client (plain-text descriptions and comments).</summary>
public sealed class JiraClient : IJira
{
    private readonly HttpClient http;
    private readonly QueueOptions options;

    /// <summary>Creates the client and configures base address and Basic authentication from the options.</summary>
    /// <param name="http">HTTP client to use.</param>
    /// <param name="options">Queue settings.</param>
    public JiraClient(HttpClient http, IOptions<QueueOptions> options)
    {
        this.http = http;
        this.options = options.Value;
        if (http.BaseAddress is null)
        {
            http.BaseAddress = new Uri(this.options.JiraBaseUrl);
        }

        var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{this.options.JiraEmail}:{this.options.JiraApiToken}"));
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);
    }

    /// <inheritdoc />
    public async Task<JiraCard?> GetNextReadyAsync(CancellationToken ct)
    {
        var jql = $"project = {options.ProjectKey} AND status = \"{options.ReadyStatus}\" ORDER BY Rank ASC";
        var url = $"rest/api/2/search/jql?jql={Uri.EscapeDataString(jql)}&fields=summary,description&maxResults=1";
        using var response = await http.GetAsync(url, ct);
        response.EnsureSuccessStatusCode();
        using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
        var issues = doc.RootElement.GetProperty("issues");
        if (issues.GetArrayLength() == 0)
        {
            return null;
        }

        var issue = issues[0];
        var fields = issue.GetProperty("fields");
        var description = fields.TryGetProperty("description", out var d) && d.ValueKind == JsonValueKind.String
            ? d.GetString()
            : null;
        return new JiraCard(issue.GetProperty("key").GetString()!, fields.GetProperty("summary").GetString()!, description);
    }

    /// <inheritdoc />
    public async Task TransitionAsync(string cardKey, string statusName, CancellationToken ct)
    {
        var path = $"rest/api/2/issue/{cardKey}/transitions";
        string? id = null;
        using (var response = await http.GetAsync(path, ct))
        {
            response.EnsureSuccessStatusCode();
            using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
            foreach (var t in doc.RootElement.GetProperty("transitions").EnumerateArray())
            {
                if (string.Equals(t.GetProperty("to").GetProperty("name").GetString(), statusName, StringComparison.OrdinalIgnoreCase))
                {
                    id = t.GetProperty("id").GetString();
                    break;
                }
            }
        }

        if (id is null)
        {
            throw new InvalidOperationException($"{cardKey} has no transition to '{statusName}'");
        }

        using var post = await http.PostAsJsonAsync(path, new { transition = new { id } }, ct);
        post.EnsureSuccessStatusCode();
    }

    /// <inheritdoc />
    public async Task CommentAsync(string cardKey, string text, CancellationToken ct)
    {
        using var response = await http.PostAsJsonAsync($"rest/api/2/issue/{cardKey}/comment", new { body = text }, ct);
        response.EnsureSuccessStatusCode();
    }
}
