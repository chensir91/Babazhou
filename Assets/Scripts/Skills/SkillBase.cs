using System.Collections.Generic;

namespace Babazhou.Skills
{
    public abstract class SkillBase
    {
        public string SkillId;
        public string SkillName;
        public SkillType Type;
        public int Range = 1;
        public int Cooldown;
        public int CurrentCooldown;

        public bool IsReady => CurrentCooldown <= 0;

        /// <summary>每个小回合开始，冷却值 -1</summary>
        public void TickCooldown()
        {
            if (CurrentCooldown > 0)
                CurrentCooldown--;
        }

        public void StartCooldown()
        {
            CurrentCooldown = Cooldown;
        }

        /// <summary>是否可以对该目标使用</summary>
        public virtual bool CanTarget(BattleManager battle, UnitBase caster, Vector2Int targetPos)
        {
            var target = battle.GetUnitAt(targetPos);
            if (target == null) return false;
            if (target.Team == caster.Team) return false;
            if (target.IsStealth && this is not AoeSkill) return false;
            return CoordinateSystem.ManhattanDist(caster.GridPosition, targetPos) <= Range;
        }

        public abstract void Execute(BattleManager battle, UnitBase caster, Vector2Int targetPos);
    }

    /// <summary>普攻</summary>
    public class BasicAttack : SkillBase
    {
        public float DamageMultiplier = 1.0f;

        public BasicAttack()
        {
            Type = SkillType.Active;
            Cooldown = 0;
            SkillName = "普攻";
        }

        public override void Execute(BattleManager battle, UnitBase caster, Vector2Int targetPos)
        {
            var target = battle.GetUnitAt(targetPos);
            if (target == null) return;

            int damage = DamageCalculator.Calc(caster, target, DamageMultiplier, DamageType.Physical);
            target.TakeDamage(damage, DamageType.Physical, caster);
            caster.OnAttackHit();
            target.OnAttacked(caster);

            battle.OnActionComplete();
        }
    }

    /// <summary>主动技能（带冷却）</summary>
    public class ActiveSkill : SkillBase
    {
        public float DamageMultiplier = 1.0f;
        public DamageType DamageType = DamageType.Physical;
        public List<StatusEffect> ApplyEffects = new();
        public bool IsDelaySkill;             // 延时技能：释放者阵亡则失效
        public bool IsPierce;                 // 贯穿伤害
        public bool IsCharge;                 // 冲锋
        public Character DelayOwner;          // 延时技能持有者

        public ActiveSkill()
        {
            Type = SkillType.Active;
        }

        public override void Execute(BattleManager battle, UnitBase caster, Vector2Int targetPos)
        {
            if (IsPierce)
            {
                // 贯穿伤害：路径上所有单位依次结算
                var path = CoordinateSystem.GetPenetrationPath(caster.GridPosition, targetPos);
                foreach (var pos in path)
                {
                    var unit = battle.GetUnitAt(pos);
                    if (unit != null && unit.Team != caster.Team)
                    {
                        int damage = DamageCalculator.Calc(caster, unit, DamageMultiplier, DamageType.Pierce);
                        unit.TakeDamage(damage, DamageType.Pierce, caster);
                        unit.OnAttacked(caster);
                    }
                }
            }
            else if (IsCharge)
            {
                // 冲锋：向前突进撞击首个接触单位
                int dx = targetPos.x - caster.GridPosition.x;
                int dy = targetPos.y - caster.GridPosition.y;
                int stepX = dx > 0 ? 1 : (dx < 0 ? -1 : 0);
                int stepY = dy > 0 ? 1 : (dy < 0 ? -1 : 0);

                Vector2Int cur = caster.GridPosition;
                while (CoordinateSystem.IsValid(new Vector2Int(cur.x + stepX, cur.y + stepY)))
                {
                    cur.x += stepX;
                    cur.y += stepY;
                    var blocker = battle.GetUnitAt(cur);
                    if (blocker != null)
                    {
                        // 撞击
                        int damage = DamageCalculator.Calc(caster, blocker, DamageMultiplier, DamageType.Physical);
                        blocker.TakeDamage(damage, DamageType.Physical, caster);
                        blocker.OnAttacked(caster);

                        // 无阻挡时攻击横向相邻
                        if (blocker.Team != caster.Team)
                        {
                            battle.GetUnitAt(new Vector2Int(cur.x + 1, cur.y))
                                ?.TakeDamage(damage, DamageType.Physical, caster);
                            battle.GetUnitAt(new Vector2Int(cur.x - 1, cur.y))
                                ?.TakeDamage(damage, DamageType.Physical, caster);
                        }
                        break;
                    }
                    caster.GridPosition = cur;
                }
            }
            else
            {
                var target = battle.GetUnitAt(targetPos);
                if (target == null) return;

                int damage = DamageCalculator.Calc(caster, target, DamageMultiplier, DamageType);
                target.TakeDamage(damage, DamageType, caster);
                target.OnAttacked(caster);

                // 附加状态效果
                foreach (var effect in ApplyEffects)
                {
                    target.StatusEffects.Add(effect);
                }
            }

            caster.OnAttackHit();
            StartCooldown();
            battle.OnActionComplete();
        }
    }

    /// <summary>AOE 技能（群体伤害，按敏捷高低依次结算）</summary>
    public class AoeSkill : ActiveSkill
    {
        public int Radius = 1;
        public bool IncludeDiagonals = true;

        public override void Execute(BattleManager battle, UnitBase caster, Vector2Int targetPos)
        {
            var area = IncludeDiagonals
                ? CoordinateSystem.GetSquare(targetPos)
                : CoordinateSystem.GetCross(targetPos);

            // 按敏捷从高到低排序
            var targets = new List<UnitBase>();
            foreach (var pos in area)
            {
                var unit = battle.GetUnitAt(pos);
                if (unit != null && unit.Team != caster.Team)
                    targets.Add(unit);
            }
            targets.Sort((a, b) => b.Agility.CompareTo(a.Agility));

            foreach (var target in targets)
            {
                if (target.IsStealth) continue; // 群体伤害依旧可以对隐匿单位造成损伤，此处按规则：群体伤害可命中隐匿单位
                int damage = DamageCalculator.Calc(caster, target, DamageMultiplier, DamageType);
                target.TakeDamage(damage, DamageType, target);
                target.OnAttacked(caster);
            }

            caster.OnAttackHit();
            StartCooldown();
            battle.OnActionComplete();
        }
    }

    /// <summary>大招（充能释放）</summary>
    public class UltimateSkill : SkillBase
    {
        public int ChargeRequired = 5;
        public float DamageMultiplier = 1.0f;
        public bool IsAutoCast;          // BD：充能满自动触发
        public bool IsPersistent;        // 持续类大招
        public int PersistTurns;

        public UltimateSkill()
        {
            Type = SkillType.Ultimate;
        }

        public bool CanUltimate(Character caster)
        {
            return caster.ChargePoints >= ChargeRequired && !caster.IsUltimateOnCooldown;
        }

        public override void Execute(BattleManager battle, UnitBase caster, Vector2Int targetPos)
        {
            var character = caster as Character;
            if (character == null || !CanUltimate(character)) return;

            character.ConsumeCharge(ChargeRequired);

            if (IsPersistent)
            {
                character.SetUltimateCooldown(true);
                // 持续效果在 PersistTurns 后结束
                battle.ScheduleDelayedAction(() =>
                {
                    character.SetUltimateCooldown(false);
                }, PersistTurns);
            }

            // 具体伤害/效果由子类或数据驱动
            var target = battle.GetUnitAt(targetPos);
            if (target != null && target.Team != caster.Team)
            {
                int damage = DamageCalculator.Calc(caster, target, DamageMultiplier, DamageType.Physical);
                target.TakeDamage(damage, DamageType.Physical, caster);
            }

            caster.OnAttackHit();
        }
    }

    /// <summary>被动技能</summary>
    public abstract class PassiveSkill : SkillBase
    {
        public PassiveSkill()
        {
            Type = SkillType.Passive;
            Cooldown = 0;
        }

        public override void Execute(BattleManager battle, UnitBase caster, Vector2Int targetPos) { }
        public abstract void OnTrigger(BattleManager battle, UnitBase owner, object context);
    }
}