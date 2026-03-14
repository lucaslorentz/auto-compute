using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace LLL.AutoCompute.EFCore.Internal;

public class SqlCaptureInterceptor : DbCommandInterceptor
{
    [ThreadStatic]
    private static bool _capturing;

    [ThreadStatic]
    public static string? CapturedSql;

    public static IDisposable StartCapture() => new CaptureScope();

    public override InterceptionResult<int> NonQueryExecuting(
        DbCommand command, CommandEventData eventData, InterceptionResult<int> result)
    {
        if (_capturing)
        {
            CapturedSql = command.CommandText;
            return InterceptionResult<int>.SuppressWithResult(0);
        }
        return result;
    }

    public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
        DbCommand command, CommandEventData eventData, InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (_capturing)
        {
            CapturedSql = command.CommandText;
            return ValueTask.FromResult(InterceptionResult<int>.SuppressWithResult(0));
        }
        return ValueTask.FromResult(result);
    }

    public override InterceptionResult<DbDataReader> ReaderExecuting(
        DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result)
    {
        if (_capturing)
        {
            CapturedSql = command.CommandText;
        }
        return result;
    }

    private class CaptureScope : IDisposable
    {
        public CaptureScope()
        {
            _capturing = true;
            CapturedSql = null;
        }

        public void Dispose()
        {
            _capturing = false;
        }
    }
}
