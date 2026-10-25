using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Pathfinder1eHelper.Models.Combat;

namespace Pathfinder1eHelper.Services;

/// <summary>
/// 角色档案的 JSON 编解码。仓储落盘与 <see cref="CharacterCloner"/> 的深拷贝共用同一套选项，
/// 避免两处各写一份后悄悄漂移（例如给枚举加转换器时只改了一边，另一边开始读不出数据）。
/// </summary>
internal static class CharacterProfileCodec
{
    /// <summary>统一的序列化选项：驼峰命名、大小写不敏感、枚举按字符串存取。</summary>
    /// <remarks>枚举按字符串存是存档兼容性的前提——数值序列化一改枚举顺序就会读错。</remarks>
    public static JsonSerializerOptions Options { get; } = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() },
    };

    public static string Serialize(CharacterProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);
        return JsonSerializer.Serialize(profile, Options);
    }

    public static CharacterProfile? Deserialize(string json) =>
        JsonSerializer.Deserialize<CharacterProfile>(json, Options);
}
