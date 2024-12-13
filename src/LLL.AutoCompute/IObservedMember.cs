using System.Linq.Expressions;

namespace LLL.AutoCompute;

public interface IObservedMember
{
    string Name { get; }
    Type InputType { get; }
    string ToDebugString();
    Expression CreateOriginalValueExpression(
        IObservedMemberAccess memberAccess,
        Expression inputExpression);
    Expression CreateCurrentValueExpression(
        IObservedMemberAccess memberAccess,
        Expression inputExpression);
    Expression CreateIncrementalOriginalValueExpression(
        IObservedMemberAccess memberAccess,
        Expression inputExpression,
        Expression incrementalContextExpression);
    Expression CreateIncrementalCurrentValueExpression(
        IObservedMemberAccess memberAccess,
        Expression inputExpression,
        Expression incrementalContextExpression);
    Task<IReadOnlyCollection<object>> GetAffectedEntitiesAsync(object input, IncrementalContext incrementalContext);
}

public interface IObservedMember<in TInput> : IObservedMember
{
    Type IObservedMember.InputType => typeof(TInput);

    Task<IReadOnlyCollection<object>> GetAffectedEntitiesAsync(TInput input, IncrementalContext incrementalContext);

    async Task<IReadOnlyCollection<object>> IObservedMember.GetAffectedEntitiesAsync(object input, IncrementalContext incrementalContext)
    {
        if (input is not TInput inputTyped)
            throw new ArgumentException($"Param {nameof(input)} should be of type {typeof(TInput)}");

        return await GetAffectedEntitiesAsync(inputTyped, incrementalContext);
    }
}