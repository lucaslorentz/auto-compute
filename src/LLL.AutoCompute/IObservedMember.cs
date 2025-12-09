using System.Linq.Expressions;

namespace LLL.AutoCompute;

/// <summary>
/// Represents an observed member (property or navigation) on an entity type.
/// </summary>
public interface IObservedMember
{
    /// <summary>The name of the member.</summary>
    string Name { get; }

    /// <summary>Returns a debug-friendly string representation of the member.</summary>
    string ToDebugString();

    /// <summary>Creates an expression that retrieves the original value of this member.</summary>
    Expression CreateOriginalValueExpression(
        ObservedMemberAccess memberAccess,
        Expression inputExpression);

    /// <summary>Creates an expression that retrieves the current value of this member.</summary>
    Expression CreateCurrentValueExpression(
        ObservedMemberAccess memberAccess,
        Expression inputExpression);
}
