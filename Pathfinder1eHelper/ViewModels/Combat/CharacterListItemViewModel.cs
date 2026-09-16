using Pathfinder1eHelper.Models.Combat;
using ReactiveUI;

namespace Pathfinder1eHelper.ViewModels.Combat;

/// <summary>角色选择列表项；名称可观察，便于重命名时刷新下拉显示。</summary>
public sealed class CharacterListItemViewModel(CharacterProfile profile) : ReactiveObject
{
    public CharacterProfile Profile { get; } = profile;

    public string Name
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    } = profile.Name;
}
