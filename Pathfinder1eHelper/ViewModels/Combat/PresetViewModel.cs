using System;
using System.Windows.Input;
using ReactiveUI;

namespace Pathfinder1eHelper.ViewModels.Combat;

/// <summary>状态预设按钮。</summary>
public sealed class PresetViewModel(string name, Action apply)
{
    public string Name { get; } = name;

    public ICommand ApplyCommand { get; } = ReactiveCommand.Create(apply);
}
