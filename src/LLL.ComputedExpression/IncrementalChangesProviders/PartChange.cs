namespace LLL.ComputedExpression.IncrementalChangesProviders;

public record class PartChange<TValue, TRootEntity>(TValue Value, IReadOnlyCollection<TRootEntity> Roots);
