namespace LLL.AutoCompute;

/// <summary>
/// Resolves CLR types to their corresponding observed entity type metadata.
/// </summary>
public interface IObservedEntityTypeResolver
{
    /// <summary>
    /// Resolves a CLR type to its observed entity type.
    /// Returns null if the type is not a tracked entity type.
    /// </summary>
    IObservedEntityType? Resolve(Type type);
}
