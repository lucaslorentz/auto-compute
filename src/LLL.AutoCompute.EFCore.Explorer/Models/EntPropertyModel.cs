namespace LLL.AutoCompute.EFCore.Explorer.Models;

public class EntPropertyModel
{
    public required string Name { get; set; }
    public required bool IsPrimaryKey { get; set; }
    public required bool IsShadow { get; set; }
    public required string ClrType { get; set; }
    public required EntComputedModel? Computed { get; set; }
    public required Dictionary<string, EntEnumItemModel>? EnumItems { get; set; }
}

public class EntEnumItemModel
{
    public required string Value { get; set; }
    public required string Label { get; set; }
}
