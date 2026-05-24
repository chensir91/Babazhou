using System.Collections.Generic;

namespace Babazhou.Units
{
    /// <summary>角色（可被禁用/选取，拥有充能系统）</summary>
    public class Character : UnitBase
    {
        public Character() { UnitType = UnitType.Character; }

        // ── 大招充能 ──
        public int ChargePoints;
        public int MaxCharge = 5;
        public bool IsUltimateOnCooldown; // 持续类大招结束后才能重新积攒

        // ── Ban/Pick ──
        public bool IsBanned;
        public bool IsPicked;

        /// <summary>获得充能点数</summary>
        public void AddCharge(int amount)
        {
            if (IsUltimateOnCooldown) return;
            ChargePoints = Mathf.Min(ChargePoints + amount, MaxCharge);
        }

        /// <summary>每大回合开始自动 +1 充能</summary>
        public void OnGrandRoundStart()
        {
            if (!IsUltimateOnCooldown)
                ChargePoints = Mathf.Min(ChargePoints + 1, MaxCharge);
        }

        /// <summary>攻击命中获得 1 充能</summary>
        public override void OnAttackHit()
        {
            AddCharge(1);
        }

        /// <summary>受击命中获得 1 充能</summary>
        public override void OnAttacked(UnitBase attacker)
        {
            AddCharge(1);
        }

        /// <summary>消耗充能释放大招</summary>
        public bool ConsumeCharge(int amount)
        {
            if (ChargePoints < amount) return false;
            ChargePoints -= amount;
            return true;
        }

        /// <summary>设置大招冷却状态（持续类大招期间禁止充能）</summary>
        public void SetUltimateCooldown(bool onCooldown)
        {
            IsUltimateOnCooldown = onCooldown;
            if (!onCooldown)
                ChargePoints = 0; // 持续效果结束，清空充能重新开始
        }
    }
}