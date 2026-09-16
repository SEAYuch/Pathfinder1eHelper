using ReactiveUI;

namespace Pathfinder1eHelper.ViewModels;

/// <summary>
/// 可参与 ReactiveUI 路由的页面视图模型。<see cref="IRoutableViewModel.HostScreen"/> 在接口上是只读的，
/// 这里显式暴露可写版本，方便 shell 在导航前赋值（页面本身不需要在构造期注入宿主）。
/// </summary>
public interface IPageViewModel : IRoutableViewModel
{
    /// <summary>宿主屏幕（由 shell 在导航前赋值）。</summary>
    new IScreen HostScreen { get; set; }
}
