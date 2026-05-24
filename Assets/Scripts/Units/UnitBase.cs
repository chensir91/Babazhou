using System.Collections.Generic;
using System.Linq;

namespace Babazhou.Units
{
    public abstract class UnitBase
    {
        public string UnitId;
        public string DisplayName;
        public UnitType UnitType;
        public Team Team;

        // ── 战斗属性 ──
        public int MaxHP;
        public int CurrentHP;
        public int BaseAttack;
        public int Agility;
        public int Armor;

        // ── 位置 ──
        public Vector2Int GridPosition;

        // ── 状态 ──
        public bool IsAlive => CurrentHP > 0;
        public List<StatusEffect> StatusEffects = new();
        public bool IsStunned => StatusEffects.Any(s => s.Tag == StatusTag.Stun);
        public bool IsBound => StatusEffects.Any(s => s.Tag == StatusTag.Bind);
        public bool IsStealth => StatusEffects.Any(s => s.Tag == StatusTag.Stealth);
        public bool IsTaunted => StatusEffects.Any(s => s.Tag == StatusTag.Taunt);
        public bool IsCharging => StatusEffects.Any(s => s.Tag == StatusTag.Charge);

        // ── 技能 ──
        public List<Skills.SkillBase> Skills = new();

        public UnitBase GetTauntSource()
        {
            var tauntEffect = StatusEffects.FirstOrDefault(s => s.Tag == StatusTag.Taunt);
            return tauntEffect?.Source;
        }

        public virtual void TakeDamage(int rawDamage, DamageType damageType, UnitBase source = null)
        {
            if (!IsAlive) return;

            int actualDamage = rawDamage;

            // 护甲抵扣（穿透伤害无视护甲）
            if (damageType != DamageType.Penetration && Armor > 0)
            {
                int armorBefore = Armor;
                Armor = Mathf.Max(0, Armor - actualDamage);
                actualDamage = Mathf.Max(0, actualDamage - armorBefore);
            }

            CurrentHP -= actualDamage;

            // 防反：受击后标记反击
            var counter = StatusEffects.FirstOrDefault(s => s.Tag == StatusTag.Counter);
            if (counter != null)
            {
                ((CounterStatus)counter).PendingRetaliations.Add(new Retaliation
                {
                    Target = source,
                    Damage = BaseAttack
                });
            }
        }

        public void Heal(int amount)
        {
            if (!IsAlive) return;
            CurrentHP = Mathf.Min(MaxHP, CurrentHP + amount);
        }

        /// <summary>攻击命中后获得充能（由子类覆盖）</summary>
        public virtual void OnAttackHit() { }

        /// <summary>受到攻击命中后处理</summary>
        public virtual void OnAttacked(UnitBase attacker) { }
    }
}