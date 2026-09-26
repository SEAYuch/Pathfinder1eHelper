namespace Pathfinder1eHelper.Models.Combat;

/// <summary>
/// 武器分类（轻型/中型/重型），对应 WotR <c>ItemEntityWeaponBlueprint.IsLight</c>。
/// 「双武器战斗」要用它判断副手是否为轻型武器（轻型可减免攻击减值）。
/// 注意与 <see cref="SizeCategory"/>（体型，用于伤害骰缩放）区分，二者不是一回事。
/// </summary>
public enum WeaponCategory
{
    Light,
    Medium,
    Heavy,
}
