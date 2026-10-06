using Ritten.Commands;
using Ritten.Contracts.FileSystem;
using Ritten.Engine;

namespace Ritten.Docker;

internal sealed class DockerClient(ICommandRunner commands) : IDocker
{
    public async Task Build(IDirectory context, string tag, string? platform = null, CancellationToken ct = default)
    {
        var command = Command.Create("docker").WithArguments("build", "--tag", tag);
        if (platform is not null)
        {
            command = command.AndArguments("--platform", platform);
        }

        await commands.Run(command.AndArguments(context.AbsolutePath).ThrowOnError(), ct);
    }

    public async Task Tag(string source, string target, CancellationToken ct = default) =>
        await commands.Run(Command.Create("docker").WithArguments("tag", source, target).ThrowOnError(), ct);

    public async Task Push(string image, CancellationToken ct = default) =>
        await commands.Run(Command.Create("docker").WithArguments("push", image).ThrowOnError(), ct);

    public async Task Login(string registry, string username, string password, CancellationToken ct = default) =>
        await commands.Run(
            Command.Create("docker")
                .WithArguments("login", "--username", username, "--password-stdin", registry)
                .WithInput(password)
                .ThrowOnError(),
            ct);

    public async Task Run(ContainerRun run, CancellationToken ct = default)
    {
        var arguments = new List<string> { "run", "--rm" };
        if (run.Network is { } network)
        {
            arguments.AddRange(["--network", network]);
        }

        foreach (var mount in run.Mounts)
        {
            arguments.AddRange(["--volume", $"{mount.Host.AbsolutePath}:{mount.Container}{(mount.ReadOnly ? ":ro" : "")}"]);
        }

        // A name alone tells docker to copy the value from its own environment, which is where
        // WithEnvironmentVariables puts it.
        foreach (var name in run.Environment.Keys)
        {
            arguments.AddRange(["--env", name]);
        }

        arguments.Add(run.Image);
        arguments.AddRange(run.Arguments);

        await commands.Run(
            Command.Create("docker").WithArguments([.. arguments]).WithEnvironmentVariables(run.Environment).ThrowOnError(),
            ct);
    }

    public async Task ComposeUp(IDirectory project, IReadOnlyDictionary<string, string>? environment = null, CancellationToken ct = default)
    {
        var command = Command.Create("docker")
            .WithArguments("compose", "--project-directory", project.AbsolutePath, "up", "-d", "--remove-orphans");
        if (environment is not null)
        {
            command = command.WithEnvironmentVariables(environment);
        }

        await commands.Run(command.ThrowOnError(), ct);
    }

    public async Task<string?> ComposeValidate(IDirectory project, IReadOnlyDictionary<string, string>? environment = null, CancellationToken ct = default)
    {
        var command = Command.Create("docker")
            .WithArguments("compose", "--project-directory", project.AbsolutePath, "config", "--quiet")
            .QuietOutput();
        if (environment is not null)
        {
            command = command.WithEnvironmentVariables(environment);
        }

        // Deliberately not ThrowOnError: what compose objected to is the answer, not a failure.
        var result = await commands.Run(command, ct);
        return result.IsSuccess ? null : Objection(result);
    }

    public async Task<Result<ComposeProject>> ComposeConfig(IDirectory project, IReadOnlyDictionary<string, string>? environment = null,
        CancellationToken ct = default)
    {
        var command = Command.Create("docker")
            .WithArguments("compose", "--project-directory", project.AbsolutePath, "config", "--format", "json")
            .QuietOutput();
        if (environment is not null)
        {
            command = command.WithEnvironmentVariables(environment);
        }

        // As with validating: a file compose cannot read is an answer the caller reports, not a failure here.
        var result = await commands.Run(command, ct);
        if (!result.IsSuccess)
        {
            return new Error(Objection(result));
        }

        return ComposeProject.Parse(result.StandardOutput);
    }

    private static string Objection(CommandResult result) =>
        result.StandardError.Trim() is { Length: > 0 } error ? error : result.StandardOutput.Trim();

    public async Task ComposeDown(IDirectory project, CancellationToken ct = default) =>
        await commands.Run(
            Command.Create("docker").WithArguments("compose", "--project-directory", project.AbsolutePath, "down").ThrowOnError(),
            ct);

    public async Task ComposeStop(IDirectory project, CancellationToken ct = default) =>
        await commands.Run(
            Command.Create("docker").WithArguments("compose", "--project-directory", project.AbsolutePath, "stop").ThrowOnError(),
            ct);

    public async Task ComposeStart(IDirectory project, CancellationToken ct = default) =>
        await commands.Run(
            Command.Create("docker").WithArguments("compose", "--project-directory", project.AbsolutePath, "start").ThrowOnError(),
            ct);

    public async Task<ContainerState?> Inspect(string container, CancellationToken ct = default)
    {
        // One template, two facts: an image reference never contains whitespace, so the pair
        // splits cleanly.
        var result = await commands.Run(
            Command.Create("docker").WithArguments("inspect", "--format", "{{.Config.Image}} {{.State.Running}}", container).QuietOutput(),
            ct);

        if (!result.IsSuccess)
        {
            return result.StandardError.Contains("no such object", StringComparison.OrdinalIgnoreCase)
                ? null
                : throw new CommandFailedException($"docker inspect {container} failed: {Objection(result)}", result);
        }
        var fields = result.StandardOutput.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (fields.Length != 2 || !bool.TryParse(fields[1], out var running))
        {
            throw new CommandFailedException($"docker inspect returned '{result.StandardOutput.Trim()}' for {container}, not an image and a state.", result);
        }

        return new ContainerState(fields[0], running);
    }

    public async Task<CommandResult> Exec(ContainerExec exec, CancellationToken ct = default)
    {
        // -i only with something to read: without input, an exec that waits on its standard input would hang.
        string[] input = exec.Input is null ? [] : ["-i"];
        string[] user = exec.User is { } name ? ["-u", name] : [];
        var command = Command.Create("docker").WithArguments(["exec", .. input, .. user, exec.Container, .. exec.Arguments]).ThrowOnError();
        if (exec.Input is { } text)
        {
            command = command.WithInput(text);
        }

        // A read is a probe: its output is the caller's to use, not the step's story.
        return await commands.Run(exec.IsReadOnly ? command.QuietOutput() : command, ct);
    }
}
