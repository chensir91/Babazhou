using System.Collections.Generic;

namespace Babazhou
{
    /// <summary>状态效果基类</summary>
    public abstract class StatusEffect
    {
        public StatusTag Tag;
        public int RemainingTurns; // 剩余回合数（-1 表示永久）
        public UnitBase Source;    // 施加者

        public bool IsExpired => RemainingTurns == 0;

        /// <summary>小回合开始时减计数</summary>
        public void OnTurnStart()
        {
            if (RemainingTurns > 0)
                RemainingTurns--;
        }

        /// <summary>回合结束时触发效果（流血等）</summary>
        public virtual void OnTurnEnd(UnitBase owner) { }

        /// <summary>大回合开始时触发（流血等）</summary>
        public virtual void OnGrandRoundStart(UnitBase owner) { }
    }

    // ── 嘲讽 ──
    public class TauntStatus : StatusEffect
    {
        public TauntStatus(UnitBase source, int turns) { Tag = StatusTag.Taunt; Source = source; RemainingTurns = turns; }
    }

    // ── 隐匿 ──
    public class StealthStatus : StatusEffect
    {
        public StealthStatus(int turns) { Tag = StatusTag.Stealth; RemainingTurns = turns; }
    }

    // ── 眩晕 ──
    public class StunStatus : StatusEffect
    {
        public StunStatus(int turns) { Tag = StatusTag.Stun; RemainingTurns = turns; }
    }

    // ── 束缚 ──
    public class BindStatus : StatusEffect
    {
        public BindStatus(int turns) { Tag = StatusTag.Bind; RemainingTurns = turns; }
    }

    // ── 蓄力 ──
    public class ChargeStatus : StatusEffect
    {
        public ChargeStatus(int turns) { Tag = StatusTag.Charge; RemainingTurns = turns; }
    }

    // ── 流血 ──
    public class BleedStatus : StatusEffect
    {
        public BleedStatus(int turns) { Tag = StatusTag.Bleed; RemainingTurns = turns; }
        public override void OnGrandRoundStart(UnitBase owner)
        {
            owner.TakeDamage(2, DamageType.Penetration);
        }
    }

    // ── 护甲 ──
    public class ArmorStatus : StatusEffect
    {
        public int ShieldAmount;
        public ArmorStatus(int shieldAmount, int turns = -1)
        {
            Tag = StatusTag.Armor; ShieldAmount = shieldAmount; RemainingTurns = turns;
        }
    }

    // ── 防反 ──
    public class CounterStatus : StatusEffect
    {
        public List<Retaliation> PendingRetaliations = new();
        public CounterStatus(int turns) { Tag = StatusTag.Counter; RemainingTurns = turns; }
    }

    public struct Retaliation
    {
        public UnitBase Target;
        public int Damage;
    }
}