namespace Pathfinder1eHelper.Models.Combat;

/// <summary>
/// 武器分类（轻型/中型/重型），对应 WotR <c>ItemEntityWeaponBlueprint.IsLight</c>。
/// 仅作为武器数据字段保留（双武器格斗规则已移除，不再影响计算）。
/// 注意与 <see cref="SizeCategory"/>（体型，用于伤害骰缩放）区分，二者不是一回事。
/// </summary>
public enum WeaponCategory
{
    Light,
    Medium,
    Heavy,
}
