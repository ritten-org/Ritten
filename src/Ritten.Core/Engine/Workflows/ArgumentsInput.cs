using System.Reflection;

namespace Ritten.Engine.Workflows;

/// <summary>
/// A property of an arguments type.
/// </summary>
/// <param name="Property">The property.</param>
/// <param name="Type">The type its value is put into the run's state under.</param>
/// <param name="Optional">Whether it can be absent, so a step can only read it as optional.</param>
internal sealed record ArgumentsInput(PropertyInfo Property, Type Type, bool Optional);