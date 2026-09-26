using Pathfinder1eHelper.Data;
using Pathfinder1eHelper.Models;
using Pathfinder1eHelper.Services;

namespace Pathfinder1eHelper.Test;

/// <summary>Integration smoke test for the shipped <c>pathfinder1e.duckdb</c> feat tables.</summary>
public class FeatDatabaseSmokeTests
{
    [Fact]
    public async Task Feat_tables_are_populated_and_mapped()
    {
        TestDatabase.SkipIfUnavailable();
        var fsql = FreeSqlFactory.CreateReadOnly(TestDatabase.Path);
        try
        {
            var repo = new FeatRepository(fsql);

            var total = await repo.CountAsync(new FeatQuery(null, null, null, null, 0, int.MaxValue));
            Assert.True(total > 1500, $"expected a populated feat table (>1500), got {total}");

            var sources = await repo.GetSourcesAsync();
            Assert.True(sources.Count > 20, $"expected many sources (core + mythic + later books), got {sources.Count}");
            Assert.Contains("CRB", sources);
            Assert.Contains("MA", sources);

            var powerAttacks = await repo.SearchAsync(new FeatQuery("Power Attack", null, null, null, 0, 50));
            var power = Assert.Single(powerAttacks, f => f.NameEn == "Power Attack" && f.Source == "CRB");
            Assert.Equal("猛力攻击", power.NameZh);
            Assert.Equal("P", power.FirstLetter);
            Assert.Equal("战斗", power.FeatType);
            Assert.True(power.IsFighterBonus);
            Assert.False(string.IsNullOrWhiteSpace(power.Benefit));

            // 「寓守于攻」同样需要规则代码，CHM 原文应含 BAB 缩放描述
            var expertises = await repo.SearchAsync(new FeatQuery("Combat Expertise", null, null, null, 0, 50));
            var expertise = Assert.Single(expertises, f => f.NameEn == "Combat Expertise" && f.Source == "CRB");
            Assert.Equal("寓守于攻", expertise.NameZh);
            Assert.Equal("战斗", expertise.FeatType);
            Assert.Contains("BAB", expertise.Benefit);
        }
        finally
        {
            fsql.Dispose();
        }
    }

    [Fact]
    public async Task Search_term_matches_prerequisites_too()
    {
        TestDatabase.SkipIfUnavailable();
        var fsql = FreeSqlFactory.CreateReadOnly(TestDatabase.Path);
        try
        {
            var repo = new FeatRepository(fsql);

            // “基础攻击加值”只出现在先决条件文本里，不是任何专长名；应仅凭先决条件命中。
            var byPrereq = await repo.SearchAsync(new FeatQuery("基础攻击加值", null, null, null, 0, 500));
            Assert.NotEmpty(byPrereq);
            Assert.DoesNotContain(byPrereq, f =>
                f.NameZh.Contains("基础攻击加值") || (f.NameEn ?? "").Contains("基础攻击加值"));

            // 命中项都确实在先决条件中包含该词。
            Assert.All(byPrereq, f => Assert.Contains("基础攻击加值", f.Prerequisites ?? ""));
        }
        finally
        {
            fsql.Dispose();
        }
    }

    [Fact]
    public async Task Source_first_letter_and_type_filters_narrow_results()
    {
        TestDatabase.SkipIfUnavailable();
        var fsql = FreeSqlFactory.CreateReadOnly(TestDatabase.Path);
        try
        {
            var repo = new FeatRepository(fsql);

            var crbA = await repo.SearchAsync(new FeatQuery(null, "CRB", "A", null, 0, 500));
            Assert.NotEmpty(crbA);
            Assert.All(crbA, (Feat f) =>
            {
                Assert.Equal("CRB", f.Source);
                Assert.Equal("A", f.FirstLetter);
            });

            var combat = await repo.SearchAsync(new FeatQuery(null, null, null, "战斗", 0, 500));
            Assert.NotEmpty(combat);
            Assert.All(combat, (Feat f) => Assert.Equal("战斗", f.FeatType));

            var types = await repo.GetTypesAsync();
            Assert.Contains("战斗", types);
            Assert.Contains("通用", types);
        }
        finally
        {
            fsql.Dispose();
        }
    }

    [Fact]
    public async Task Mythic_feats_are_present()
    {
        TestDatabase.SkipIfUnavailable();
        var fsql = FreeSqlFactory.CreateReadOnly(TestDatabase.Path);
        try
        {
            var repo = new FeatRepository(fsql);

            var mythic = await repo.SearchAsync(new FeatQuery(null, "MA", null, null, 0, 500));
            Assert.True(mythic.Count > 100, $"expected mythic feats (>100), got {mythic.Count}");

            var accursed = Assert.Single(mythic, f => f.NameEn == "Accursed Hex");
            Assert.Equal("诅咒巫术", accursed.NameZh);
            Assert.Equal("神话", accursed.FeatType);
            Assert.False(string.IsNullOrWhiteSpace(accursed.Prerequisites));
            Assert.False(string.IsNullOrWhiteSpace(accursed.Benefit));
        }
        finally
        {
            fsql.Dispose();
        }
    }

    [Fact]
    public async Task Later_book_feats_are_present()
    {
        TestDatabase.SkipIfUnavailable();
        var fsql = FreeSqlFactory.CreateReadOnly(TestDatabase.Path);
        try
        {
            var repo = new FeatRepository(fsql);

            // 极限荒野（UW）等后续书籍页面（专长*.htm）已并入。
            var uw = await repo.SearchAsync(new FeatQuery(null, "UW", null, null, 0, 500));
            Assert.True(uw.Count > 50, $"expected Ultimate Wilderness feats (>50), got {uw.Count}");

            var ambush = Assert.Single(uw, f => f.NameEn == "Ambush Awareness");
            Assert.Equal("突袭警惕", ambush.NameZh);
            Assert.False(string.IsNullOrWhiteSpace(ambush.Benefit));
        }
        finally
        {
            fsql.Dispose();
        }
    }

    [Fact]
    public async Task Story_feats_put_goal_sections_in_benefit_not_prerequisites()
    {
        TestDatabase.SkipIfUnavailable();
        var fsql = FreeSqlFactory.CreateReadOnly(TestDatabase.Path);
        try
        {
            var repo = new FeatRepository(fsql);

            var story = await repo.SearchAsync(new FeatQuery(null, "UCa", null, "故事", 0, 200));
            Assert.NotEmpty(story);

            var accursed = story.First(f => f.NameEn == "Accursed");
            // 先决条件只应含真正的先决条件；即时收益/专长目标/完成收益属详述。
            Assert.NotNull(accursed.Prerequisites);
            Assert.DoesNotContain("即时收益", accursed.Prerequisites);
            Assert.DoesNotContain("专长目标", accursed.Prerequisites);
            Assert.DoesNotContain("完成收益", accursed.Prerequisites);
            Assert.Contains("完成收益", accursed.Benefit);
        }
        finally
        {
            fsql.Dispose();
        }
    }

    [Fact]
    public async Task Prerequisite_marker_variant_is_recognized()
    {
        TestDatabase.SkipIfUnavailable();
        var fsql = FreeSqlFactory.CreateReadOnly(TestDatabase.Path);
        try
        {
            var repo = new FeatRepository(fsql);

            // 部分专长的先决条件写作“前提条件：…”（灰色描述内），须识别为 Prerequisites。
            var rows = await repo.SearchAsync(new FeatQuery("感应传送", null, null, null, 0, 20));
            var feat = rows.First(f => f.NameZh == "感应传送");
            Assert.NotNull(feat.Prerequisites);
            Assert.Contains("感知", feat.Prerequisites);
            Assert.DoesNotContain("前提条件", feat.Flavor ?? string.Empty);
        }
        finally
        {
            fsql.Dispose();
        }
    }

    [Fact]
    public async Task Feat_buff_table_exposes_structured_effects()
    {
        TestDatabase.SkipIfUnavailable();
        var fsql = FreeSqlFactory.CreateReadOnly(TestDatabase.Path);
        try
        {
            var repo = new FeatRepository(fsql);

            var dodge = await repo.GetBuffsForFeatAsync("Dodge", "闪避");
            var row = Assert.Single(dodge);
            Assert.Equal("Dodge", row.BonusType);
            Assert.Equal("ArmorClass", row.Target);
            Assert.Equal(1, row.Value);
            Assert.NotNull(row.FeatId);

            var dodgeByZh = await repo.GetBuffsForFeatAsync(null, "闪避");
            Assert.Single(dodgeByZh);

            // 「寓守于攻」与「猛力攻击」一样是规则计算型条目：数值留空，由计算层按 BAB 展开。
            var expertise = await repo.GetBuffsForFeatAsync("Combat Expertise", "寓守于攻");
            var expertiseRow = Assert.Single(expertise);
            Assert.Equal("CombatExpertise", expertiseRow.Kind);
            Assert.Equal("ArmorClass", expertiseRow.Target);
            Assert.Equal(0, expertiseRow.Value);
            Assert.NotNull(expertiseRow.FeatId);

            var powerAttack = await repo.GetBuffsForFeatAsync("Power Attack", "猛力攻击");
            Assert.Equal("PowerAttack", Assert.Single(powerAttack).Kind);

            // 白鹤流派：白鹤拳拆成「规则行 + AC 行」两行（见 feat_buffs.sql 注释）
            var craneStyle = await repo.GetBuffsForFeatAsync("Crane Style", "白鹤拳");
            Assert.Equal(2, craneStyle.Count);
            var craneStyleRule = Assert.Single(craneStyle, b => b.Kind == "CraneStyle");
            Assert.Equal("Attack", craneStyleRule.Target);
            Assert.Equal(0, craneStyleRule.Value);
            Assert.NotNull(craneStyleRule.FeatId);
            var craneStyleAc = Assert.Single(craneStyle, b => b.Kind is null);
            Assert.Equal("Dodge", craneStyleAc.BonusType);
            Assert.Equal("ArmorClass", craneStyleAc.Target);
            Assert.Equal(1, craneStyleAc.Value);

            var craneWing = await repo.GetBuffsForFeatAsync("Crane Wing", "白鹤亮翅");
            Assert.Equal(4, Assert.Single(craneWing).Value);

            // 双武器格斗：规则行，数值留空
            var twf = await repo.GetBuffsForFeatAsync("Two-Weapon Fighting", "双武器格斗");
            var twfRow = Assert.Single(twf);
            Assert.Equal("TwoWeaponFighting", twfRow.Kind);
            Assert.Equal("Attack", twfRow.Target);
            Assert.Equal(0, twfRow.Value);
            Assert.NotNull(twfRow.FeatId);
        }
        finally
        {
            fsql.Dispose();
        }
    }

    /// <summary>
    /// UI 出处的中文名/英文名在解析时残留过碎括号（zh 以悬空的 <c>（</c> 收尾、en 以 <c>）</c>
    /// 收头/收尾，如“特技施法者（”+“Acrobatic Spellcaster）”），已在参考库中清洗；重建管线若再
    /// 产出此类残留，在此失败。
    /// </summary>
    [Fact]
    public async Task Feat_names_have_no_dangling_brackets()
    {
        TestDatabase.SkipIfUnavailable();
        var fsql = FreeSqlFactory.CreateReadOnly(TestDatabase.Path);
        try
        {
            var repo = new FeatRepository(fsql);
            var all = await repo.SearchAsync(new FeatQuery(null, null, null, null, 0, 5000));
            Assert.True(all.Count > 1000, $"expected a populated feat table, got {all.Count}");

            var broken = all
                .Where(f => f.NameZh.EndsWith('（') || f.NameZh.EndsWith('(')
                    || f.NameEn is { } en && (en.Contains('（')
                        || en.StartsWith('）') || en.StartsWith(')')
                        || en.EndsWith('）') || en.EndsWith('（')))
                .Select(f => $"{f.Source}/{f.Id}: zh=[{f.NameZh}] en=[{f.NameEn}]")
                .ToList();

            Assert.True(broken.Count == 0, "dangling/mismatched brackets still in feat names:\n" + string.Join("\n", broken));
        }
        finally
        {
            fsql.Dispose();
        }
    }
}
