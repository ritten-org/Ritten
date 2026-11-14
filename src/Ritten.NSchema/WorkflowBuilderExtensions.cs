using Ritten.Engine;

namespace Ritten.NSchema;

/// <summary>
/// Registers the NSchema domain.
/// </summary>
public static class WorkflowBuilderExtensions
{
    extension(IWorkflowBuilder builder)
    {
        /// <summary>
        /// Adds the NSchema client and its rehearsal.
        /// </summary>
        public IWorkflowBuilder AddNSchema(Action<NSchemaOptions>? configure = null)
        {
            builder.Services.AddNSchema(configure);
            builder.Decorators.Decorate<INSchema, DryRunNSchema>();
            return builder;
        }
    }
}
