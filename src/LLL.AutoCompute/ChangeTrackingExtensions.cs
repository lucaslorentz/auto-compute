namespace LLL.AutoCompute;

public static class ChangeTrackingExtensions
{
    public static T AsComputedUntracked<T>(this T value)
    {
        return value;
    }
    
    public static T AsComputedTracked<T>(this T value)
    {
        return value;
    }
}