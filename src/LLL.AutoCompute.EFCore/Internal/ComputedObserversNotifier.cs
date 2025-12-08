using System.Collections.Concurrent;

namespace LLL.AutoCompute.EFCore.Internal;

public class ComputedObserversNotifier : IComputedObserversNotifier
{
    private readonly ConcurrentQueue<Func<Task>> _notifications = [];

    public void AddNotification(Func<Task> notification)
    {
        _notifications.Enqueue(notification);
    }

    public async Task Notify()
    {
        while (_notifications.TryDequeue(out var notification))
        {
            await notification();
        }
    }
}
