using System.Linq.Expressions;
using LLL.AutoCompute.EFCore;
using LLL.AutoCompute.EFCore.Internal;
using LLL.AutoCompute.EntityContexts;
using Microsoft.EntityFrameworkCore;

namespace LLL.AutoCompute.ChangesProviders;

public class EFCoreChangesProvider<TEntity, TChange>(
    IUnboundChangesProvider<IEFCoreComputedInput, TEntity, TChange> unboundChangesProvider,
    DbContext dbContext,
    ChangeMemory<TEntity, TChange> memory
) : IChangesProvider<TEntity, TChange>
    where TEntity : class
{
    LambdaExpression IChangesProvider.Expression => unboundChangesProvider.Expression;
    public EntityContext EntityContext => unboundChangesProvider.EntityContext;
    public IChangeCalculation<TChange> ChangeCalculation => unboundChangesProvider.ChangeCalculation;

    public async Task<IReadOnlyDictionary<TEntity, TChange>> GetChangesAsync()
    {
        var observedMembers = unboundChangesProvider.EntityContext.GetAllObservedMembers()
            .OfType<EFCoreObservedMember>()
            .ToArray();

        var changesToProcess = new EFCoreChangeset();

        foreach (var observedMember in observedMembers)
            await observedMember.CollectChangesAsync(dbContext, changesToProcess);

        var input = new EFCoreComputedInput(dbContext, changesToProcess);

        return await unboundChangesProvider.GetChangesAsync(input, memory);
    }
}