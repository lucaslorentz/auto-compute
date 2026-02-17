using System.Reflection;

namespace LLL.AutoCompute.EFCore.Explorer;

public class AutoComputeExplorerOptions
{
    public Func<MethodInfo, bool> MethodFilter { get; set; } = m =>
        m.IsPublic
        && !m.IsStatic
        && m.GetParameters().Length == 0
        && !m.IsSpecialName;
}
