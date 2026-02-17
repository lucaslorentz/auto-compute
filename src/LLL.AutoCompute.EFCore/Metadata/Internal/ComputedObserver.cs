using Microsoft.EntityFrameworkCore;

namespace LLL.AutoCompute.EFCore.Metadata.Internal;

public abstract class ComputedObserver(
    string name,
    IComputedChangesProvider changesProvider)
    : ComputedBase(changesProvider)
{
    public string Name => name;
    public abstract Task<Func<Task>?> CreateObserverNotifier(ComputedInput input);
}

public class ComputedObserver<TEntity, TChange>(
    string name,
    IComputedChangesProvider<TEntity, TChange> changesProvider,
    Func<ComputedChangeEventData<TEntity, TChange>, Task> callback
) : ComputedObserver(name, changesProvider)
    where TEntity : class
{
    public new IComputedChangesProvider<TEntity, TChange> ChangesProvider => changesProvider;

    public override string ToDebugString()
    {
        return $"ComputedObserver({Name})";
    }

    public override async Task<Func<Task>?> CreateObserverNotifier(ComputedInput input)
    {
        var changes = await changesProvider.GetChangesAsync(input, null);
        if (changes.Count == 0)
            return null;

        var eventData = new ComputedChangeEventData<TEntity, TChange>
        {
            DbContext = input.Get<DbContext>(),
            Changes = changes
        };

        return () => callback(eventData);
    }
}
