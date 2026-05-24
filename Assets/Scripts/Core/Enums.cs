namespace Babazhou
{
    public enum Team { Player1, Player2 }

    public enum UnitType { Character, Summon }

    public enum SkillType { Passive, Active, Ultimate }

    public enum DamageType
    {
        Physical,      // 普通物理
        Penetration,   // 穿透（无视护甲）
        Pierce         // 贯穿（路径伤害）
    }

    public enum AilmentType
    {
        NeuroDamage,   // 神经损伤：2伤害+眩晕
        FearShock,     // 恐惧震慑：4伤害
        BurnDamage     // 燃烧损伤：立即2伤害+3回合持续
    }

    public enum StatusTag
    {
        Taunt,         // 嘲讽(F)
        Stealth,       // 隐匿
        Stun,          // 眩晕(SY)
        Bind,          // 束缚(SF)
        Charge,        // 蓄力
        Bleed,         // 流血
        Armor,         // 护甲(HD)
        Counter        // 防反标记
    }

    public enum BattlePhase
    {
        Preparation,        // 准备阶段（布阵）
        BanPick,            // 禁用/选将
        PlayerTurn,         // 玩家小回合
        Settlement,         // 大回合结算
        GameOver
    }
}