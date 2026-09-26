using System.Collections.Generic;
using Pathfinder1eHelper.Models.Combat;

namespace Pathfinder1eHelper.Services;

/// <summary>一条修饰的叠加结果明细。<see cref="Included"/> 为 false 表示被同描述符更高/更低者或护甲-负重规则压制。</summary>
public sealed record ModifierContribution(Modifier Modifier, bool Included, string? Note);

/// <summary>叠加求值结果：合计值与全部修饰明细。</summary>
public sealed record ModifierEvaluation(int Total, IReadOnlyList<ModifierContribution> Contributions);
