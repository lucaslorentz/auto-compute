using System.Linq.Expressions;

namespace LLL.AutoCompute;

/// <summary>
/// Locates observed member accesses within expressions during expression analysis.
/// </summary>
public interface IObservedMemberAccessLocator
{
    /// <summary>
    /// Attempts to identify an observed member access from an expression node.
    /// Returns null if the node does not represent an observed member access.
    /// </summary>
    ObservedMemberAccess? GetObservedMemberAccess(Expression node);
}
