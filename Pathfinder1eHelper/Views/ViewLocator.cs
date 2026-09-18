using System;
using System.Collections.Generic;
using Pathfinder1eHelper.ViewModels.Pages;
using Pathfinder1eHelper.Views.Pages;
using ReactiveUI;
using Splat;

namespace Pathfinder1eHelper.Views;

/// <summary>
/// Explicit-mapping <see cref="IViewLocator"/> for ReactiveUI routing (<c>RoutedViewHost</c>):
/// view-model type → view factory, without any reflection (NativeAOT/裁剪安全，重命名即编译期可见)。
/// 新增可路由页面时在 <see cref="Map"/> 里加一行即可。
/// </summary>
public sealed class ViewLocator : IViewLocator
{
    private static readonly Dictionary<Type, Func<IViewFor>> Map = new()
    {
        [typeof(SpellsViewModel)] = Create<SpellsView>,
        [typeof(CombatViewModel)] = Create<CombatView>,
        [typeof(MonstersViewModel)] = Create<MonstersView>,
    };

    public IViewFor<TViewModel>? ResolveView<TViewModel>()
        where TViewModel : class =>
        ResolveView<TViewModel>(null);

    // 单视图/VM，contract 预留扩展（按需升级为 (Type, contract) 复合键）。
    public IViewFor<TViewModel>? ResolveView<TViewModel>(string? contract)
        where TViewModel : class =>
        ResolveByType(typeof(TViewModel)) as IViewFor<TViewModel>;

    public IViewFor? ResolveView(object? instance) => ResolveView(instance, null);

    public IViewFor? ResolveView(object? instance, string? contract) =>
        instance is null ? null : ResolveByType(instance.GetType());

    private static IViewFor? ResolveByType(Type viewModelType) =>
        Map.TryGetValue(viewModelType, out var factory) ? factory() : null;

    private static TView Create<TView>()
        where TView : class, new() =>
        Locator.Current.GetService<TView>() ?? new TView();
}