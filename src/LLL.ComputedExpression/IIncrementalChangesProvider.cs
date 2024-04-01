namespace LLL.ComputedExpression;

public interface IIncrementalChangesProvider
{
    Task<IDictionary<object, object?>> GetIncrementalChangesAsync(object input);
}
