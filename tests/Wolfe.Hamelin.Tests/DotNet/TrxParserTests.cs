using System.Text;
using Hamelin.FileSystem;
using Wolfe.Hamelin.DotNet;

namespace Wolfe.Hamelin.Tests.DotNet;

public class TrxParserTests
{
    private const string SampleTrx =
        """
        <?xml version="1.0" encoding="utf-8"?>
        <TestRun xmlns="http://microsoft.com/schemas/VisualStudio/TeamTest/2010">
          <Results>
            <UnitTestResult testName="Suite.PassingTest" outcome="Passed" />
            <UnitTestResult testName="Suite.FailingTest" outcome="Failed">
              <Output>
                <ErrorInfo>
                  <Message>Expected 1 but was 2.</Message>
                </ErrorInfo>
              </Output>
            </UnitTestResult>
            <UnitTestResult testName="Suite.SkippedTest" outcome="NotExecuted" />
          </Results>
          <ResultSummary outcome="Failed">
            <Counters total="3" executed="2" passed="1" failed="1" notExecuted="1" />
          </ResultSummary>
        </TestRun>
        """;

    [Fact]
    public async Task ReadTestResults_ReadsTheCountersAndFailures()
    {
        var client = new DotNetClient();
        var file = Substitute.For<IFile>();
        file.OpenRead().Returns(_ => new MemoryStream(Encoding.UTF8.GetBytes(SampleTrx)));

        var run = await client.ReadTestResults(file, TestContext.Current.CancellationToken);

        run.Passed.ShouldBe(1);
        run.Failed.ShouldBe(1);
        run.Skipped.ShouldBe(1);
        run.Total.ShouldBe(3);
        run.Failures.ShouldHaveSingleItem().ShouldBe(new TestFailure("Suite.FailingTest", "Expected 1 but was 2."));
    }
}
