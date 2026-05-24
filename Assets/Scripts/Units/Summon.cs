namespace Babazhou.Units
{
    /// <summary>召唤物</summary>
    public class Summon : UnitBase
    {
        public Summon() { UnitType = UnitType.Summon; }

        /// <summary>拥有指令效果的召唤物，行动归属召唤师回合。null 表示独立回合。</summary>
        public Character Summoner;
        public bool HasCommand => Summoner != null;

        /// <summary>召唤物可以不设置生命值（始终存活，直到被主动移除）</summary>
        public bool IsImmortal;

        public override void TakeDamage(int rawDamage, DamageType damageType, UnitBase source = null)
        {
            if (IsImmortal) return;
            base.TakeDamage(rawDamage, damageType, source);
        }
    }
}