using Microsoft.EntityFrameworkCore.Metadata;

namespace LLL.AutoCompute.EFCore.Internal;

public abstract class EFCoreEntityMember
{
    public abstract IPropertyBase PropertyBase { get; }
}