using System.Net;
using ClaudeQueue.Jira;
using ClaudeQueue.Tests.Fakes;
using Microsoft.Extensions.Options;

namespace ClaudeQueue.Tests.Jira;

public class JiraClientTests
{
    private static JiraClient Client(FakeHandler h) =>
        new(new HttpClient(h), Options.Create(new QueueOptions { JiraBaseUrl = "https://example.atlassian.net" }));

    [Fact] public async Task Next_ready_uses_spec_jql_and_maps_card()
    {
        var h = new FakeHandler().Respond(HttpStatusCode.OK,
            """{"issues":[{"key":"SCRUM-7","fields":{"summary":"Health","description":"Queue-Repo: a/b"}}]}""");
        var card = await Client(h).GetNextReadyAsync(CancellationToken.None);
        Assert.Equal(new JiraCard("SCRUM-7", "Health", "Queue-Repo: a/b"), card);
        Assert.Contains("project = SCRUM AND status = \"Ready for Claude\" ORDER BY Rank ASC",
            Uri.UnescapeDataString(h.Requests[0].RequestUri!.Query));
    }

    [Fact] public async Task Next_ready_returns_null_when_queue_empty() =>
        Assert.Null(await Client(new FakeHandler().Respond(HttpStatusCode.OK, """{"issues":[]}""")).GetNextReadyAsync(default));

    [Fact] public async Task Next_ready_maps_null_description()
    {
        var h = new FakeHandler().Respond(HttpStatusCode.OK,
            """{"issues":[{"key":"SCRUM-8","fields":{"summary":"S","description":null}}]}""");
        Assert.Null((await Client(h).GetNextReadyAsync(default))!.Description);
    }

    [Fact] public async Task Requests_carry_basic_auth()
    {
        var h = new FakeHandler().Respond(HttpStatusCode.OK, """{"issues":[]}""");
        var http = new HttpClient(h);
        var client = new JiraClient(http, Options.Create(new QueueOptions { JiraEmail = "a@b.c", JiraApiToken = "tok" }));
        await client.GetNextReadyAsync(default);
        Assert.Equal("Basic " + Convert.ToBase64String("a@b.c:tok"u8.ToArray()), h.Requests[0].Headers.Authorization!.ToString());
    }

    [Fact] public async Task GetStatus_reads_status_name_of_issue()
    {
        var h = new FakeHandler().Respond(HttpStatusCode.OK, """{"fields":{"status":{"name":"In Progress"}}}""");
        Assert.Equal("In Progress", await Client(h).GetStatusAsync("SCRUM-7", default));
        Assert.Equal("/rest/api/2/issue/SCRUM-7?fields=status", h.Requests[0].RequestUri!.PathAndQuery);
    }

    [Fact] public async Task Transition_posts_matching_transition_id()
    {
        var h = new FakeHandler()
            .Respond(HttpStatusCode.OK, """{"transitions":[{"id":"21","to":{"name":"In Progress"}},{"id":"31","to":{"name":"In Review"}}]}""")
            .Respond(HttpStatusCode.NoContent, "");
        await Client(h).TransitionAsync("SCRUM-7", "in review", default);
        Assert.Equal("""{"transition":{"id":"31"}}""", h.Bodies[1]);
    }

    [Fact] public async Task Missing_transition_names_card_and_status()
    {
        var h = new FakeHandler().Respond(HttpStatusCode.OK, """{"transitions":[{"id":"21","to":{"name":"In Progress"}}]}""");
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => Client(h).TransitionAsync("SCRUM-7", "In Review", default));
        Assert.Equal("SCRUM-7 has no transition to 'In Review'", ex.Message);
    }

    [Fact] public async Task Comment_posts_plain_text_body()
    {
        var h = new FakeHandler().Respond(HttpStatusCode.Created, "{}");
        await Client(h).CommentAsync("SCRUM-7", "Started 2026-10-29 21:00:00", default);
        Assert.Equal("""{"body":"Started 2026-10-29 21:00:00"}""", h.Bodies[0]);
    }

    [Fact] public async Task Non_success_status_throws_HttpRequestException() =>
        await Assert.ThrowsAsync<HttpRequestException>(() =>
            Client(new FakeHandler().Respond(HttpStatusCode.Unauthorized, "")).CommentAsync("SCRUM-7", "x", default));
}
