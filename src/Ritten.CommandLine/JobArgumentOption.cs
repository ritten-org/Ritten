using System.CommandLine;

namespace Ritten.CommandLine;

/// <summary>
/// A job's argument as the command line sees it.
/// </summary>
/// <param name="Option">The option to add to the command.</param>
/// <param name="Parse">Records what was parsed for this argument.</param>
internal sealed record JobArgumentOption(Option Option, Action<ParseResult, JobArgumentsBuilder> Parse);
