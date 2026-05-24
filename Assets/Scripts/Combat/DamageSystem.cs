namespace Babazhou.Combat
{
    public static class DamageCalculator
    {
        /// <summary>基础伤害计算：基础攻击力 × 伤害倍率，向下取整</summary>
        public static int Calc(UnitBase attacker, UnitBase defender, float multiplier, DamageType damageType)
        {
            float raw = attacker.BaseAttack * multiplier;
            int damage = Mathf.FloorToInt(raw);
            return Mathf.Max(0, damage);
        }
    }

    /// <summary>损伤叠层系统</summary>
    public class AilmentStacker
    {
        private readonly Dictionary<UnitBase, Dictionary<AilmentType, int>> _stacks = new();

        public void AddStack(UnitBase target, AilmentType type, int amount, BattleManager battle)
        {
            if (!_stacks.ContainsKey(target))
                _stacks[target] = new Dictionary<AilmentType, int>();

            if (!_stacks[target].ContainsKey(type))
                _stacks[target][type] = 0;

            _stacks[target][type] += amount;

            if (_stacks[target][type] >= 10)
            {
                TriggerAilment(target, type, battle);
                _stacks[target][type] = 0;
            }
        }

        public int GetStacks(UnitBase target, AilmentType type)
        {
            if (!_stacks.ContainsKey(target)) return 0;
            if (!_stacks[target].ContainsKey(type)) return 0;
            return _stacks[target][type];
        }

        private void TriggerAilment(UnitBase target, AilmentType type, BattleManager battle)
        {
            switch (type)
            {
                case AilmentType.NeuroDamage:
                    // 神经损伤：2点伤害 + 眩晕
                    target.TakeDamage(2, DamageType.Penetration);
                    target.StatusEffects.Add(new StunStatus(1));
                    break;

                case AilmentType.FearShock:
                    // 恐惧震慑：4点高额伤害
                    target.TakeDamage(4, DamageType.Penetration);
                    break;

                case AilmentType.BurnDamage:
                    // 燃烧损伤：立即2点伤害 + 后续3个大回合每回合1点
                    target.TakeDamage(2, DamageType.Penetration);
                    target.StatusEffects.Add(new BurnAilmentStatus(3));
                    break;
            }
        }
    }

    /// <summary>燃烧损伤持续状态</summary>
    public class BurnAilmentStatus : StatusEffect
    {
        public BurnAilmentStatus(int turns)
        {
            Tag = StatusTag.Bleed; // 复用流血标签
            RemainingTurns = turns;
        }

        public override void OnGrandRoundStart(UnitBase owner)
        {
            owner.TakeDamage(1, DamageType.Penetration);
        }
    }
}