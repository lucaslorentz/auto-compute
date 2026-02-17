namespace LLL.AutoCompute.EFCore.Explorer.Models;

public class EntListModel
{
    public EntDataModel[] Entities { get; set; } = [];
    public object? NextPageToken { get; set; }
    public bool HasNextPage { get; set; }
}
