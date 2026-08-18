using Ritten.Contracts;
using Ritten.Engine;

namespace Ritten.Reporting;

/// <summary>
/// Provides level-specific write methods for <see cref="IWorkflowLog"/>.
/// </summary>
public static class WorkflowLogExtensions
{
    extension(IWorkflowLog log)
    {
        /// <summary>
        /// Writes a progress message that is always visible.
        /// </summary>
        public void Status(string message) => log.Log(WorkflowLogLevel.Status, message);

        /// <summary>
        /// Writes a detail message that is hidden in quiet mode.
        /// </summary>
        public void Detail(string message) => log.Log(WorkflowLogLevel.Detail, message);

        /// <summary>
        /// Writes a diagnostic message that is only shown with --verbose.
        /// </summary>
        public void Verbose(string message, Exception? exception = null) => log.Log(WorkflowLogLevel.Verbose, message, exception);

        /// <summary>
        /// Writes a note that an action was deliberately not taken.
        /// </summary>
        public void Skipped(string message) => log.Log(WorkflowLogLevel.Skipped, message);

        /// <summary>
        /// Writes a warning about something that went wrong without failing the workflow.
        /// </summary>
        public void Warning(string message, Exception? exception = null) => log.Log(WorkflowLogLevel.Warning, message, exception);

        /// <summary>
        /// Writes an error. Step failures are reported through <see cref="StepResult"/> instead;
        /// this is for failures that happen outside a step.
        /// </summary>
        public void Error(string message, Exception? exception = null) => log.Log(WorkflowLogLevel.Error, message, exception);

        /// <summary>
        /// Writes an error.
        /// </summary>
        public void Error(Error error) => log.Log(WorkflowLogLevel.Error, error.Message, error.Cause);

        /// <summary>
        /// Writes multiple errors.
        /// </summary>
        public void Errors(IEnumerable<Error> errors)
        {
            foreach (var error in errors)
            {
                log.Error(error);
            }
        }
    }
}
