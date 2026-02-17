namespace LLL.AutoCompute.EFCore.Explorer.Models;

public class ConsistencyModel(ComputedMemberConsistency consistency)
{
    public int ConsistentCount => consistency.ConsistentCount;
    public int InconsistentCount => consistency.InconsistentCount;
    public int TotalCount => consistency.TotalCount;
    public decimal ConsistencyPercentage => consistency.ConsistencyPercentage;
}
