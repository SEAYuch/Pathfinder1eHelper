using System;
using System.Collections.Generic;
using Pathfinder1eHelper.Models.Combat;

namespace Pathfinder1eHelper.Services;

/// <summary>多角色战斗档案的持久化（每个角色一个 JSON 文件）。</summary>
public interface ICharacterRepository
{
    string DirectoryPath { get; }

    IReadOnlyList<CharacterProfile> LoadAll();

    void Save(CharacterProfile profile);

    void Delete(Guid id);
}
