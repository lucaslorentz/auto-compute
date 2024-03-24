namespace LLL.Computed;

public static class StopTrackingExtensions
{
    public static T AsComputedUntracked<T>(this T value)
    {
        return value;
    }
}