using System.Collections.Specialized;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Pathfinder1eHelper.Test;

/// <summary>
/// 等待视图模型的异步结果。用属性变更/集合变更信号代替固定 <c>Task.Delay</c>，
/// 避免“睡够时间”式的偶发失败（尤其在不同机器负载下）。
/// </summary>
internal static class TestSignals
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(5);

    /// <summary>等待 <paramref name="source"/> 的某个属性变更后满足 <paramref name="predicate"/>。</summary>
    public static Task WaitForPropertyAsync(
        INotifyPropertyChanged source,
        string propertyName,
        Func<bool> predicate,
        [CallerArgumentExpression(nameof(propertyName))] string? expression = null)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(predicate);

        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        PropertyChangedEventHandler handler = (_, e) =>
        {
            if (e.PropertyName == propertyName && predicate())
            {
                completion.TrySetResult();
            }
        };

        source.PropertyChanged += handler;
        // 变更可能已经发生（命令在挂订阅前就完成），先判一次避免永久等待。
        if (predicate())
        {
            completion.TrySetResult();
        }

        return AwaitAndDetachAsync(completion.Task, () => source.PropertyChanged -= handler, expression);
    }

    /// <summary>等待集合变更（增/删/清空）后满足 <paramref name="predicate"/>。</summary>
    public static Task WaitForCollectionAsync(
        INotifyCollectionChanged source,
        Func<bool> predicate,
        [CallerArgumentExpression(nameof(predicate))] string? expression = null)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(predicate);

        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        NotifyCollectionChangedEventHandler handler = (_, _) =>
        {
            if (predicate())
            {
                completion.TrySetResult();
            }
        };

        source.CollectionChanged += handler;
        if (predicate())
        {
            completion.TrySetResult();
        }

        return AwaitAndDetachAsync(completion.Task, () => source.CollectionChanged -= handler, expression);
    }

    private static async Task AwaitAndDetachAsync(Task task, Action detach, string? expression)
    {
        try
        {
            await task.WaitAsync(DefaultTimeout);
        }
        catch (TimeoutException)
        {
            throw new TimeoutException($"Timed out waiting for property change: {expression}");
        }
        finally
        {
            detach();
        }
    }
}
