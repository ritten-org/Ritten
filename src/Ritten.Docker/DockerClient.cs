using Ritten.Commands;
using Ritten.Contracts.FileSystem;

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

    public async Task<ContainerState> Inspect(string container, CancellationToken ct = default)
    {
        // One template, two facts: an image reference never contains whitespace, so the pair
        // splits cleanly.
        var result = await commands.Run(
            Command.Create("docker").WithArguments("inspect", "--format", "{{.Config.Image}} {{.State.Running}}", container).QuietOutput().ThrowOnError(),
            ct);
        var fields = result.StandardOutput.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (fields.Length != 2 || !bool.TryParse(fields[1], out var running))
        {
            throw new CommandFailedException($"docker inspect returned '{result.StandardOutput.Trim()}' for {container}, not an image and a state.", result);
        }

        return new ContainerState(fields[0], running);
    }
}
