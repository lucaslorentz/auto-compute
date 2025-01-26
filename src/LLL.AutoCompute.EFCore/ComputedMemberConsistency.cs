namespace LLL.AutoCompute.EFCore;

public class ComputedMemberConsistency(int consistentCount, int inconsistentCount)
{
    public int ConsistentCount => consistentCount;
    public int InconsistentCount => inconsistentCount;
    public int TotalCount => consistentCount + inconsistentCount;
    public decimal ConsistencyPercentage => TotalCount == 0 ? 100m : 100m * ConsistentCount / TotalCount;
}