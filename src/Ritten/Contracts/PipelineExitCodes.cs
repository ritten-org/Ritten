namespace Ritten.Contracts;

internal static class PipelineExitCodes
{
    /// <summary>
    /// Indicates the pipeline terminated successfully.
    /// </summary>
    public const int Success = 0;

    /// <summary>
    /// Indicates the pipeline stopped because of an error.
    /// </summary>
    public const int StoppedOnError = -1;

    /// <summary>
    /// Indicates the pipeline had an error, but was configured to continue.
    /// </summary>
    public const int ContinuedAfterError = -2;

    /// <summary>
    /// Indicates the pipeline stopped due to outside cancellation.
    /// </summary>
    public const int StoppedAfterCancel = -3;
}
