using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Pathfinder1eHelper.Models.Combat;

namespace Pathfinder1eHelper.Services;

/// <summary>通过 JSON 往返深拷贝角色档案（新 Id，其余字段原样保留）。</summary>
public static class CharacterCloner
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() },
    };

    public static CharacterProfile Duplicate(CharacterProfile source)
    {
        ArgumentNullException.ThrowIfNull(source);

        var json = JsonSerializer.Serialize(source, SerializerOptions);
        var clone = JsonSerializer.Deserialize<CharacterProfile>(json, SerializerOptions) ?? new CharacterProfile();
        clone.Id = Guid.NewGuid();
        return clone;
    }
}
