namespace LLL.ComputedExpression;

public static class StopTrackingExtensions
{
    public static T AsComputedUntracked<T>(this T value)
    {
        return value;
    }
}