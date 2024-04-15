using System.Reflection;

namespace LLL.ComputedExpression;

public static class TypeExtensions
{
    public static object GetDefaultEqualityComparer(this Type type)
    {
        return typeof(EqualityComparer<>)
            .MakeGenericType(type)
            .GetProperty("Default", BindingFlags.Static | BindingFlags.Public)!
            .GetValue(null, null)!;
    }
}