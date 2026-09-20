using Microsoft.Extensions.Options;
using Ritten.Commands;

namespace Ritten.OpenTofu;

/// <summary>
/// OpenTofu as the <c>tofu</c> on the path.
/// </summary>
internal sealed class OpenTofuClient(ICommandRunner commands, IOptions<OpenTofuOptions> options) : IOpenTofu
{
    public async Task Init(CancellationToken ct = default) =>
        await commands.Run(Tofu("init", "-input=false").AndArguments(VarFile).ThrowOnError(), ct);

    public async Task<TofuPlanResult> Plan(CancellationToken ct = default)
    {
        // -detailed-exitcode splits the answer three ways: 0 nothing to do, 2 changes, anything
        // else broken. Without it a plan that cannot reach its backend and a plan with nothing
        // to do are the same exit code, and a drift check built on that reports quiet either way.
        var result = await commands.Run(
            Tofu("plan", "-input=false", "-lock=false", "-no-color", "-detailed-exitcode").AndArguments(VarFile),
            ct);

        return result.ExitCode.Value switch
        {
            0 => new TofuPlanResult(false, result.StandardOutput),
            2 => new TofuPlanResult(true, result.StandardOutput),
            _ => throw new CommandFailedException($"tofu plan exited with code {result.ExitCode}.", result)
        };
    }

    public async Task Apply(CancellationToken ct = default) =>
        await commands.Run(Tofu("apply", "-input=false", "-auto-approve", "-no-color").AndArguments(VarFile).ThrowOnError(), ct);

    public async Task<IReadOnlyList<string>?> VerifyFormatting(CancellationToken ct = default)
    {
        // `fmt -check -list` names the files rather than only failing, which is the difference
        // between a report a reader can act on and one that sends them back to a terminal.
        var result = await commands.Run(Tofu("fmt", "-recursive", "-check", "-list=true", "-no-color").QuietOutput(), ct);
        if (result.IsSuccess)
        {
            return null;
        }

        var files = result.StandardOutput
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        // An exit code with no listing is a failure to read the files at all, not a formatting one.
        return files.Length > 0 ? files : throw new CommandFailedException("tofu fmt could not read the root module.", result);
    }

    private string[] VarFile => options.Value.VarFile is { Length: > 0 } file ? [$"-var-file={file}"] : [];

    // -chdir comes before the subcommand, which is the one place OpenTofu cares about order.
    private Command Tofu(params string[] arguments) =>
        Command.Create("tofu").WithArguments(options.Value.Root is { Length: > 0 } root
            ? [$"-chdir={root}", .. arguments]
            : arguments);
}
