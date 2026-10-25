using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Pathfinder1eHelper.Infrastructure;
using Pathfinder1eHelper.Models.Combat;

namespace Pathfinder1eHelper.Services;

/// <summary>
/// 将角色档案以 <c>{id}.json</c> 存于用户数据目录；首次运行会把旧版单文件
/// <c>character.json</c> 迁移为多角色文件并重命名为 <c>.bak</c>。不触碰只读参考库。
/// </summary>
public sealed class JsonCharacterRepository(string directoryPath) : ICharacterRepository
{
    private const string LegacyFileName = "character.json";

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
                var profile = CharacterProfileCodec.Deserialize(File.ReadAllText(file));
                if (profile is null)
                {
                    continue;
                }

                profile.ApplyDefaults();
                profiles.Add(profile);
            }
            catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
            {
                // 跳过损坏/不可读的文件，不影响其余角色；记一条日志，避免角色“无声消失”后无从查证。
                AppLog.Error($"JsonCharacterRepository.LoadAll({file})", ex);
            }
        }

        return profiles
            .OrderBy(p => p.Name, StringComparer.CurrentCulture)
            .ToList();
    }

    /// <summary>
    /// 写入单个角色档案。先写同目录临时文件再原子替换目标文件——直接
    /// <c>File.WriteAllText</c> 会在写一半时被中断（进程被杀、断电）留下截断的 JSON，
    /// 而 <see cref="LoadAll"/> 会把解析失败的文件当作损坏文件跳过，角色相当于凭空消失。
    /// </summary>
    public void Save(CharacterProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);

        Directory.CreateDirectory(DirectoryPath);

        var path = FilePathFor(profile.Id);
        var json = CharacterProfileCodec.Serialize(profile);
        // 临时文件名带随机后缀：多个写者（自动保存与显式保存）撞同一目标时不会互相踩临时文件。
        var temporaryPath = Path.Combine(DirectoryPath, $".{profile.Id:N}.{Guid.NewGuid():N}.tmp");

        try
        {
            using (var stream = new FileStream(
                       temporaryPath,
                       FileMode.CreateNew,
                       FileAccess.Write,
                       FileShare.None))
            {
                using var writer = new StreamWriter(stream);
                writer.Write(json);
                writer.Flush();
                // 落盘后再替换，保证断电时看到的要么是旧档、要么是完整新档。
                stream.Flush(flushToDisk: true);
            }

            File.Move(temporaryPath, path, overwrite: true);
        }
        finally
        {
            TryDelete(temporaryPath);
        }
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

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // 清理临时文件失败不影响保存结果，残留的 .tmp 也不会被 LoadAll 的 *.json 匹配到。
        }
    }

    private void MigrateLegacyFile()
    {
        var legacyPath = Path.Combine(DirectoryPath, LegacyFileName);
        if (!File.Exists(legacyPath))
        {
            return;
        }

        try
        {
            var profile = CharacterProfileCodec.Deserialize(File.ReadAllText(legacyPath));
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
            AppLog.Error($"JsonCharacterRepository.MigrateLegacyFile({legacyPath})", ex);
        }
    }
}
