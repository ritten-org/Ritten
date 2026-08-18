namespace Ritten.Contracts;

/// <summary>
/// The console narrative: step lifecycle and the log channel render through one writer, so the
/// two faces are one service. The active runtime supplies the implementation, because how a run
/// should read depends on where it's running.
/// </summary>
public interface IPipelineConsole : IProgressReporter, IPipelineLog;
