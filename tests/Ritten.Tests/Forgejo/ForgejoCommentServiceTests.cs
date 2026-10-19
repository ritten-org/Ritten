using System.Net;
using System.Text;
using Microsoft.Extensions.Options;
using Ritten.Forgejo;
using Ritten.Reporting;

namespace Ritten.Tests.Forgejo;

public class ForgejoCommentServiceTests
{
    [Fact]
    public void MarkerOf_NamesTheWorkflow()
    {
        ForgejoCommentService.MarkerOf(new ReportKey("CI")).ShouldBe("<!-- ritten:ci -->");
    }

    [Fact]
    public void MarkerOf_NamesTheSubjectToo_SoEachSubjectKeepsItsOwnComment()
    {
        ForgejoCommentService.MarkerOf(new ReportKey("CI", "caddy/infrastructure")).ShouldBe("<!-- ritten:ci:caddy-infrastructure -->");
    }

    [Fact]
    public async Task List_ReadsTheKeyOfEveryReportComment()
    {
        var comments = """
            [
              { "id": 1, "body": "<!-- ritten:ci -->\n## ✅ CI" },
              { "id": 2, "body": "<!-- ritten:ci:caddy-infrastructure -->\n## ✅ CI · caddy/infrastructure" },
              { "id": 3, "body": "Looks good to me." }
            ]
            """;

        var keys = await Service(comments).List(TestContext.Current.CancellationToken);

        keys.ShouldBe([new ReportKey("CI"), new ReportKey("CI", "caddy/infrastructure")]);
    }

    [Fact]
    public async Task List_IsEmptyWithoutAToken()
    {
        var keys = await Service("[]", token: null).List(TestContext.Current.CancellationToken);

        keys.ShouldBeEmpty();
    }

    private static ForgejoCommentService Service(string comments, string? token = "token")
    {
        var options = new ForgejoActionsOptions
        {
            Repository = "tom/lab",
            ApiUrl = "https://code.example.com/api/v1/",
            Token = token,
            PullRequestNumber = 42
        };

        var client = new HttpClient(new Respond(comments)) { BaseAddress = new Uri(options.ApiUrl) };
        var clients = Substitute.For<IHttpClientFactory>();
        clients.CreateClient(ForgejoCommentService.HttpClientName).Returns(client);
        return new ForgejoCommentService(Options.Create(options), clients);
    }

    private sealed class Respond(string json) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json, Encoding.UTF8, "application/json") });
    }
}
