using Ritten.Commands;

namespace Ritten.Tests.Commands;

public class EnvironmentVariableNameTests
{
    [Theory]
    [InlineData("PATH")]
    [InlineData("_private")]
    [InlineData("TF_VAR_region")]
    [InlineData("Config__ClientSecret")]
    public void Parse_TakesAName(string value) => EnvironmentVariableName.Parse(value).Value.ShouldNotBeNull().Value.ShouldBe(value);

    [Theory]
    [InlineData("")]
    [InlineData("1PASSWORD")]
    [InlineData("MY-VAR")]
    [InlineData("A B")]
    [InlineData("A=B")]
    public void Parse_RefusesWhatNoShellCouldName(string value) =>
        EnvironmentVariableName.Parse(value).Errors.ShouldNotBeNull().ShouldHaveSingleItem().Message.ShouldBe(
            $"'{value}' is not an environment variable name: a letter or underscore, then letters, digits and underscores.");

    [Fact]
    public void From_ThrowsForWhatIsNoName() => Should.Throw<ArgumentException>(() => EnvironmentVariableName.From("MY-VAR"));

    [Fact]
    public void NamesAreEqualByValue_AndCaseMatters()
    {
        EnvironmentVariableName.From("HOME").ShouldBe(EnvironmentVariableName.From("HOME"));
        EnvironmentVariableName.From("HOME").ShouldNotBe(EnvironmentVariableName.From("home"));
    }
}
