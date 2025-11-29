namespace LLL.AutoCompute.EFCore.Metadata.Internal;

public static class AutoComputeAnnotationNames
{
    public const string Prefix = "AutoCompute:";
    public const string TempPrefix = Prefix + "Temp:";
    public const string MemberFactory = TempPrefix + "MemberFactory";
    public const string Member = TempPrefix + "Member";
    public const string ObserversFactories = TempPrefix + "ObserversFactories";
    public const string Observers = TempPrefix + "Observers";
    public const string ExpressionAnalyzer = TempPrefix + "ExpressionAnalyzer";
    public const string ObservedEntityType = TempPrefix + "ObservedEntityType";
    public const string ObservedMember = TempPrefix + "ObservedMember";
    public const string AllComputeds = TempPrefix + "AllComputeds";
    public const string AllMembers = TempPrefix + "AllMembers";
    public const string AllObservers = TempPrefix + "AllObservers";
    public const string ConsistencyFilter = TempPrefix + "ConsistencyFilter";
    public const string ConsistencyCheck = TempPrefix + "ConsistencyCheck";
}
