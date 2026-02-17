namespace LLL.AutoCompute.EFCore.Explorer.Models;

public class EntComputedModel
{
    public required string Name { get; set; }
    public required string Entity { get; set; }
    public required string Member { get; set; }
    public required string Expression { get; set; }
    public required EntObservedMemberModel[]? Dependencies { get; set; }
    public required EntObservedMemberModel[]? AllEntitiesDependencies { get; set; }
    public required EntObservedMemberModel[]? LoadedEntitiesDependencies { get; set; }
    public required FlowGraphModel EntityContextGraph { get; set; }
}
