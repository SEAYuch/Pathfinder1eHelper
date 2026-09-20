using System.Collections.ObjectModel;
using System.Linq;
using Pathfinder1eHelper.Models.Combat;
using Pathfinder1eHelper.Services;

namespace Pathfinder1eHelper.ViewModels.Combat;

/// <summary>
/// 角色列表与增删改：负责持久化、副本/新角色命名，以及“至少保留一个角色”的约束。
/// 从战斗页 VM 中抽离，使页面 VM 只负责编辑状态与呈现。
/// </summary>
public sealed class CharacterRoster(ICharacterRepository repository)
{
    private readonly ICharacterRepository _repository = repository;

    public ObservableCollection<CharacterListItemViewModel> Characters { get; } = [];

    /// <summary>从仓储载入全部角色（按仓储顺序）。</summary>
    public void Load()
    {
        Characters.Clear();
        foreach (var profile in _repository.LoadAll())
        {
            Characters.Add(new CharacterListItemViewModel(profile));
        }
    }

    /// <summary>返回第一个角色；列表为空时新建一个。</summary>
    public CharacterListItemViewModel EnsureAtLeastOne() =>
        Characters.Count > 0 ? Characters[0] : Create();

    /// <summary>新建角色（自动命名并落盘），返回新列表项。</summary>
    public CharacterListItemViewModel Create()
    {
        var profile = NewProfile();
        _repository.Save(profile);

        var item = new CharacterListItemViewModel(profile);
        Characters.Add(item);
        return item;
    }

    /// <summary>以 <paramref name="source"/> 的当前值深拷贝出一个副本并落盘。</summary>
    public CharacterListItemViewModel Duplicate(CharacterProfile source)
    {
        var clone = CharacterCloner.Duplicate(source);
        clone.Name = NextDuplicateName(source.Name);
        _repository.Save(clone);

        var item = new CharacterListItemViewModel(clone);
        Characters.Add(item);
        return item;
    }

    /// <summary>删除指定角色并返回应选中的角色（删空时自动新建）。</summary>
    public CharacterListItemViewModel Delete(CharacterListItemViewModel item)
    {
        _repository.Delete(item.Profile.Id);
        Characters.Remove(item);
        return EnsureAtLeastOne();
    }

    private string NextDuplicateName(string baseName)
    {
        var index = 1;
        string name;
        do
        {
            index++;
            name = index == 2 ? $"{baseName} 副本" : $"{baseName} 副本 {index - 1}";
        }
        while (Characters.Any(c => c.Name == name));

        return name;
    }

    private string NextCharacterName()
    {
        var index = Characters.Count + 1;
        string name;
        do
        {
            name = $"新角色 {index++}";
        }
        while (Characters.Any(c => c.Name == name));

        return name;
    }

    private CharacterProfile NewProfile()
    {
        var profile = new CharacterProfile { Name = NextCharacterName() };
        profile.ApplyDefaults();
        return profile;
    }
}
