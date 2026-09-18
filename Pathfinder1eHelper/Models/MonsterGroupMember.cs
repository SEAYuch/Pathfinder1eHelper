using FreeSql.DataAnnotations;

namespace Pathfinder1eHelper.Models;

/// <summary>FreeSql entity mapped to <c>monster_group_members</c>（绪论 ↔ 怪物 多对多关系）。</summary>
[Table(Name = "monster_group_members", DisableSyncStructure = true)]
public sealed class MonsterGroupMember
{
    [Column(Name = "group_id", IsPrimary = true)] public int GroupId { get; set; }

    [Column(Name = "monster_id", IsPrimary = true)] public int MonsterId { get; set; }
}
