using Ritten.Core.Runner;

namespace Ritten.Tests.Core.Runner;

public class DefaultPipelineStateTests
{
    [Fact]
    public void Get_MissingType_ReturnsNull()
    {
        var state = new DefaultPipelineState();

        state.Get<List<string>>().ShouldBeNull();
    }

    [Fact]
    public void GetAndSet_Primitive_WorksCorrectly()
    {
        var state = new DefaultPipelineState();

        state.Set(1234.56m);

        state.Get<decimal>().ShouldBe(1234.56m);
    }

    [Fact]
    public void GetAndSet_ReferenceType_WorksCorrectly()
    {
        var state = new DefaultPipelineState();
        List<string> value = ["Hello", "World"];

        state.Set(value);

        state.Get<List<string>>().ShouldBe(value);
    }

    [Fact]
    public void Set_ReplacesExistingValue()
    {
        var state = new DefaultPipelineState();

        state.Set(100m);
        state.Set(200m);

        state.Get<decimal>().ShouldBe(200m);
    }
}
