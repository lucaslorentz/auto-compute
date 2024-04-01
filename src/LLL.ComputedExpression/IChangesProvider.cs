namespace LLL.ComputedExpression;

public interface IChangesProvider
{
    Task<IDictionary<object, (object? OriginalValue, object? NewValue)>> GetChangesAsync(object input);
}
