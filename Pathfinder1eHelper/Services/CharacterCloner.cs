using System;
using Pathfinder1eHelper.Models.Combat;

namespace Pathfinder1eHelper.Services;

/// <summary>通过 JSON 往返深拷贝角色档案（新 Id，其余字段原样保留）。</summary>
public static class CharacterCloner
{
    public static CharacterProfile Duplicate(CharacterProfile source)
    {
        ArgumentNullException.ThrowIfNull(source);

        // 与仓储共用同一套序列化选项：新增字段时无需维护第二份映射表，
        // 也不会因为两处选项漂移而丢掉某些字段（例如只给一边加了枚举转换器）。
        var json = CharacterProfileCodec.Serialize(source);
        var clone = CharacterProfileCodec.Deserialize(json) ?? new CharacterProfile();
        clone.Id = Guid.NewGuid();
        return clone;
    }
}
