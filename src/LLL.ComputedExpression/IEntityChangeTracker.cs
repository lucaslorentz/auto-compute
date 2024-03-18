using System.Linq.Expressions;

namespace LLL.Computed;

public interface IEntityChangeTracker
{
    void TrackChanges(Expression node, IComputedExpressionAnalysis analysis);
}

public interface IEntityChangeTracker<in TInput> : IEntityChangeTracker
{
}
