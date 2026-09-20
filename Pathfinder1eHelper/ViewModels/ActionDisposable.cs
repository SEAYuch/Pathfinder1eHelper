using System;
using System.Threading;

namespace Pathfinder1eHelper.ViewModels;

/// <summary>
/// 把回调包装成 <see cref="IDisposable"/>（用于 <c>WhenActivated</c> 的停用钩子，
/// 不依赖 System.Reactive 的 <c>Disposable.Create</c>）。回调最多执行一次。
/// </summary>
internal sealed class ActionDisposable(Action action) : IDisposable
{
    private Action? _action = action;

    public static ActionDisposable Create(Action action) => new(action);

    public void Dispose() => Interlocked.Exchange(ref _action, null)?.Invoke();
}
