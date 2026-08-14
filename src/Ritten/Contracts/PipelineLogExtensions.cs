namespace Ritten.Contracts;

/// <summary>
/// Provides level-specific write methods for <see cref="IPipelineLog"/>.
/// </summary>
public static class PipelineLogExtensions
{
    extension(IPipelineLog log)
    {
        /// <summary>
        /// Writes a progress message that is always visible.
        /// </summary>
        public void Status(string message) => log.Log(PipelineLogLevel.Status, message);

        /// <summary>
        /// Writes a detail message that is hidden in quiet mode.
        /// </summary>
        public void Detail(string message) => log.Log(PipelineLogLevel.Detail, message);

        /// <summary>
        /// Writes a diagnostic message that is only shown with --verbose.
        /// </summary>
        public void Verbose(string message) => log.Log(PipelineLogLevel.Verbose, message);

        /// <summary>
        /// Writes a warning about something that went wrong without failing the pipeline.
        /// </summary>
        public void Warning(string message) => log.Log(PipelineLogLevel.Warning, message);

        /// <summary>
        /// Writes an error. Step failures are reported through <see cref="StepResult"/> instead;
        /// this is for failures that happen outside a step.
        /// </summary>
        public void Error(string message) => log.Log(PipelineLogLevel.Error, message);
    }
}
