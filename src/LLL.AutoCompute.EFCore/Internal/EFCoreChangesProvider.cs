using LLL.AutoCompute.EFCore;
using LLL.AutoCompute.EFCore.Internal;
using Microsoft.EntityFrameworkCore;

namespace LLL.AutoCompute.ChangesProviders;

public class EFCoreChangesProvider<TEntity, TChange>(
    IComputedChangesProvider<TEntity, TChange> unboundChangesProvider,
    DbContext dbContext)
    where TEntity : class
{
    private readonly ChangeMemory<TEntity, TChange> _memory = new();
    private readonly EFCoreObservedMember[] _observedMembers = unboundChangesProvider.EntityContext
        .GetAllObservedMembers()
        .OfType<EFCoreObservedMember>()
        .ToArray();

    private readonly EFCoreObservedEntityType[] _observedEntityTypes = unboundChangesProvider.EntityContext
        .GetAllObservedEntityTypes()
        .OfType<EFCoreObservedEntityType>()
        .ToArray();

    public async Task<IReadOnlyDictionary<TEntity, TChange>> GetChangesAsync()
    {
        var changesToProcess = new EFCoreChangeset();

        foreach (var observedMember in _observedMembers)
            await observedMember.CollectChangesAsync(dbContext, changesToProcess);

        foreach (var observedEntityType in _observedEntityTypes)
            await observedEntityType.CollectChangesAsync(dbContext, changesToProcess);

        var input = new ComputedInput()
            .Set(dbContext)
            .Set(changesToProcess);

        return await unboundChangesProvider.GetChangesAsync(input, _memory);
    }
}
