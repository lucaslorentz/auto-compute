namespace LLL.AutoCompute.EFCore.Metadata.Internal;

public static class ComputedAnnotationNames
{
    private const string Prefix = "Computed:";
    public const string Factories = Prefix + "Factories";
    public const string Computeds = Prefix + "Computeds";
    public const string ExpressionAnalyzer = Prefix + "ExpressionAnalyzer";
    public const string EntityMember = Prefix + "EntityMember";
    public const string SortedComputeds = Prefix + "SortedComputeds";
}
