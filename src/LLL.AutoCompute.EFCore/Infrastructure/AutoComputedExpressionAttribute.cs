using System.ComponentModel;

namespace LLL.AutoCompute.EFCore.Infrastructure;

/// <summary>Identifies expression factories emitted by the AutoCompute source generator.</summary>
[EditorBrowsable(EditorBrowsableState.Never)]
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public sealed class AutoComputedExpressionAttribute(string propertyName, string fieldName) : Attribute
{
    public string PropertyName { get; } = propertyName;
    public string FieldName { get; } = fieldName;
}
