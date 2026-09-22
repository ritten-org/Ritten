using Ritten.Engine.Runtimes;

namespace Ritten.Tests.Engine.Runtimes;

public class DefaultSecretProviderProviderTests
{
    [Theory]
    [InlineData("https://example.com")]
    [InlineData("/Volumes/Data2/restic")]
    public async Task ALiteralComesBackAsItWas(string value)
    {
        (await new DefaultSecretProviderProvider().Resolve(value, TestContext.Current.CancellationToken)).ShouldBe(value);
    }

    [Theory]
    [InlineData("op://Vault/item/field")]
    [InlineData("bws://id")]
    public async Task AReferenceIsRefusedWithWhatToRegister(string value)
    {
        // A reference that reaches a run with no provider must fail, not travel as a literal
        // that happens to look like a URL — so the engine's own answer knows the schemes.
        var failure = await Should.ThrowAsync<InvalidOperationException>(
            () => new DefaultSecretProviderProvider().Resolve(value, TestContext.Current.CancellationToken));

        failure.Message.ShouldContain(value);
        failure.Message.ShouldContain("AddOnePassword");
    }
}
