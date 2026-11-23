using Ritten.Commands;

namespace Ritten.Tests.Commands;

public class EnvironmentVariablesTests
{
    private static readonly EnvironmentVariableName Region = EnvironmentVariableName.From("TF_VAR_region");
    private static readonly EnvironmentVariableName Token = EnvironmentVariableName.From("TF_VAR_token");

    [Fact]
    public void With_SetsAVariable_LeavingTheOriginalAsItWas()
    {
        var empty = EnvironmentVariables.Empty;

        var set = empty.With(Region, "eu-west-2").With(Region, "eu-west-1");

        set[Region].ShouldBe("eu-west-1");
        set.Count.ShouldBe(1);
        empty.ShouldBeEmpty();
    }

    [Fact]
    public void Without_RemovesAVariable()
    {
        var set = EnvironmentVariables.Empty.With(Region, "eu-west-2").With(Token, "t");

        set.Without(Region).Keys.ShouldBe([Token]);
        set.Without(EnvironmentVariableName.From("ABSENT")).ShouldBe(set);
    }

    [Fact]
    public void Merge_KeepsBoth_TheLatersValueReplacingTheEarliers()
    {
        var earlier = EnvironmentVariables.Empty.With(Region, "eu-west-2").With(Token, "from-file");
        var later = EnvironmentVariables.Empty.With(Token, "given");

        var merged = earlier.Merge(later);

        merged.ShouldBe(EnvironmentVariables.Empty.With(Region, "eu-west-2").With(Token, "given"));
        (earlier + later).ShouldBe(merged);
    }

    [Fact]
    public void From_TakesNamesWrittenInCode_TheLaterOfARepeatedName()
    {
        var set = EnvironmentVariables.From([KeyValuePair.Create("A", "1"), KeyValuePair.Create("B", "2"), KeyValuePair.Create("A", "3")]);

        set.ShouldBe(EnvironmentVariables.Empty.With(EnvironmentVariableName.From("A"), "3").With(EnvironmentVariableName.From("B"), "2"));
        Should.Throw<ArgumentException>(() => EnvironmentVariables.From([KeyValuePair.Create("NOT-A-NAME", "x")]));
    }

    [Fact]
    public void SetsAreEqual_WhateverOrderTheyWereBuiltIn()
    {
        var one = EnvironmentVariables.Empty.With(Region, "r").With(Token, "t");
        var other = EnvironmentVariables.Empty.With(Token, "t").With(Region, "r");

        one.ShouldBe(other);
        one.GetHashCode().ShouldBe(other.GetHashCode());
        one.ShouldNotBe(other.With(Token, "different"));
    }

    [Fact]
    public void ToString_NamesTheVariables_NeverTheirValues() =>
        EnvironmentVariables.Empty.With(Token, "s3cret").With(Region, "r").ToString().ShouldBe("[TF_VAR_region, TF_VAR_token]");
}
