using Microsoft.EntityFrameworkCore.Metadata;

namespace LLL.AutoCompute.EFCore.Metadata.Internal;

public static class DesignTimeComputedStore
{
    /// <summary>
    /// Factories collected during model finalization (before temp annotations are removed).
    /// Available for use after the model is finalized.
    /// </summary>
    public static IReadOnlyList<(IProperty Property, ComputedMemberFactory<IProperty> Factory)>? Factories { get; set; }

    /// <summary>
    /// Computed members created from the factories after model finalization.
    /// </summary>
    public static IReadOnlyList<ComputedMember>? ComputedMembers { get; set; }

    public static IComputedExpressionAnalyzer? ExpressionAnalyzer { get; set; }
}
