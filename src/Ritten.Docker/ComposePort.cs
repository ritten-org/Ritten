namespace Ritten.Docker;

/// <summary>
/// One of a service's published ports.
/// </summary>
/// <param name="Target">The container's port.</param>
/// <param name="Published">The host's port, or null when Docker picks one or the file gave a range.</param>
/// <param name="HostIp">The host address it is published on, or null for every one.</param>
/// <param name="Protocol"><c>tcp</c> or <c>udp</c>.</param>
public sealed record ComposePort(int Target, int? Published, string? HostIp, string Protocol);
