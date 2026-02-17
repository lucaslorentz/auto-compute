namespace LLL.AutoCompute.EFCore.Explorer.Models;

public class EntDetailsModel
{
    public required string Name { get; set; }
    public required EntPropertyModel[] Properties { get; set; }
    public required EntNavigationModel[] Navigations { get; set; }
    public required EntMethodModel[] Methods { get; set; }
    public required EntObserverModel[] Observers { get; set; }
}
