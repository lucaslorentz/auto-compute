using LLL.AutoCompute.EFCore.Internal;

namespace LLL.AutoCompute.EFCore.Metadata.Internal;

public abstract class ComputedObserver(
    IComputedChangesProvider changesProvider)
    : ComputedBase(changesProvider)
{
    public abstract Task<Func<Task>?> CreateObserverNotifier(EFCoreComputedInput input);
}

public class ComputedObserver<TEntity, TChange>(
    IComputedChangesProvider<EFCoreComputedInput, TEntity, TChange> changesProvider,
    Func<ComputedChangeEventData<TEntity, TChange>, Task> callback
) : ComputedObserver(changesProvider)
    where TEntity : class
{
    public new IComputedChangesProvider<EFCoreComputedInput, TEntity, TChange> ChangesProvider => changesProvider;

    public override string ToDebugString()
    {
        return "ComputedObserver";
    }

    public override async Task<Func<Task>?> CreateObserverNotifier(EFCoreComputedInput input)
    {
        var changes = await changesProvider.GetChangesAsync(input, null);
        if (changes.Count == 0)
            return null;

        var eventData = new ComputedChangeEventData<TEntity, TChange>
        {
            DbContext = input.DbContext,
            Changes = changes
        };

        return () => callback(eventData);
    }
}