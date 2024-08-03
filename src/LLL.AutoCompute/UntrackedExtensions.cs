namespace LLL.AutoCompute;

public static class UntrackedExtensions
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