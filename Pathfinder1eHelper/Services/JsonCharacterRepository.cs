using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Pathfinder1eHelper.Models.Combat;

namespace Pathfinder1eHelper.Services;

/// <summary>
/// 将角色档案以 <c>{id}.json</c> 存于用户数据目录；首次运行会把旧版单文件
/// <c>character.json</c> 迁移为多角色文件并重命名为 <c>.bak</c>。不触碰只读参考库。
/// </summary>
public sealed class JsonCharacterRepository(string directoryPath) : ICharacterRepository
{
    private const string LegacyFileName = "character.json";

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() },
    };

    public static string DefaultDirectory { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "Pathfinder1eHelper",
        "characters");

    public string DirectoryPath { get; } = directoryPath;

    public IReadOnlyList<CharacterProfile> LoadAll()
    {
        Directory.CreateDirectory(DirectoryPath);
        MigrateLegacyFile();

        var profiles = new List<CharacterProfile>();
        foreach (var file in Directory.EnumerateFiles(DirectoryPath, "*.json"))
        {
            try
            {
                var profile = JsonSerializer.Deserialize<CharacterProfile>(File.ReadAllText(file), SerializerOptions);
                if (profile is null)
                {
                    continue;
                }

                profile.ApplyDefaults();
                profiles.Add(profile);
            }
            catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
            {
                // 跳过损坏/不可读的文件，不影响其余角色。
            }
        }

        return profiles
            .OrderBy(p => p.Name, StringComparer.CurrentCulture)
            .ToList();
    }

    public void Save(CharacterProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);

        Directory.CreateDirectory(DirectoryPath);
        File.WriteAllText(FilePathFor(profile.Id), JsonSerializer.Serialize(profile, SerializerOptions));
    }

    public void Delete(Guid id)
    {
        var path = FilePathFor(id);
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }

    public string FilePathFor(Guid id) => Path.Combine(DirectoryPath, $"{id:N}.json");

    private void MigrateLegacyFile()
    {
        var legacyPath = Path.Combine(DirectoryPath, LegacyFileName);
        if (!File.Exists(legacyPath))
        {
            return;
        }

        try
        {
            var profile = JsonSerializer.Deserialize<CharacterProfile>(File.ReadAllText(legacyPath), SerializerOptions);
            if (profile is not null)
            {
                profile.ApplyDefaults();
                Save(profile);
            }

            File.Move(legacyPath, legacyPath + ".bak", overwrite: true);
        }
        catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
        {
            // 迁移失败时保留旧文件，下次启动重试。
        }
    }
}
