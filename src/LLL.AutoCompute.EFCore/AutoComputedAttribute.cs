namespace LLL.AutoCompute.EFCore;

/// <summary>
/// Persists and maintains the expression of a getter-only property during SaveChanges.
/// The declaring class must be partial. The getter continues to evaluate its C# expression.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
public sealed class AutoComputedAttribute : Attribute;
