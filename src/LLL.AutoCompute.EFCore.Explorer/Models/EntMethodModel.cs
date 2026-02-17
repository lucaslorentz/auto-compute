namespace LLL.AutoCompute.EFCore.Explorer.Models;

public class EntMethodModel
{
    public required string Name { get; set; }
    public required string ClrType { get; set; }
    public required Dictionary<string, EntEnumItemModel>? EnumItems { get; set; }
}
