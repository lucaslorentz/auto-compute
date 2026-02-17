namespace LLL.AutoCompute.EFCore.Explorer.Models;

public class EntNavigationModel
{
    public required string Name { get; set; }
    public required bool IsCollection { get; set; }
    public required string TargetEntity { get; set; }
    public string? FilterKey { get; set; }
    public required EntComputedModel? Computed { get; set; }
}
