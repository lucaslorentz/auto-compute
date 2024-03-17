using System.Linq.Expressions;

namespace L3.Computed;

public interface IEntityChangeTracker
{
    void TrackChanges(Expression node, IComputedExpressionAnalysis analysis);
}

public interface IEntityChangeTracker<in TInput> : IEntityChangeTracker
{
}
