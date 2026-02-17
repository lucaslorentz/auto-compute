namespace LLL.AutoCompute.EFCore.Explorer.Models;

public class FlowGraphModel
{
    public required List<FlowNodeModel> Nodes { get; set; }
    public required List<FlowEdgeModel> Edges { get; set; }
}

public class FlowNodeModel
{
    public required string Id { get; set; }
    public required string Type { get; set; }
    public required FlowNodeDataModel Data { get; set; }
}

public class FlowNodeDataModel
{
    public required string Label { get; set; }
    public required string EntityType { get; set; }
    public required string Expression { get; set; }
    public required bool IsTrackingChanges { get; set; }
    public required string PropagationTarget { get; set; }
    public required bool CanResolveLoadedEntities { get; set; }
    public List<string>? Observing { get; set; }
}

public class FlowEdgeModel
{
    public required string Id { get; set; }
    public required string Source { get; set; }
    public required string Target { get; set; }
    public string? Label { get; set; }
}
