using System;
using System.Collections.Generic;
using System.Linq;

namespace Babazhou
{
    public class BattleManager
    {
        // ── 单例 ──
        public static BattleManager Instance;

        // ── 棋盘状态 ──
        public Dictionary<Vector2Int, UnitBase> Board = new();
        public List<Character> Player1Roster = new();
        public List<Character> Player2Roster = new();
        public List<Summon> Summons = new();

        // ── 回合状态 ──
        public BattlePhase Phase;
        public Team CurrentTurn;
        public int GrandRound; // 大回合计数
        public int MiniTurnIndex; // 当前小回合索引
        public List<UnitBase> ActingOrder = new(); // 按玩家自定义顺序排列的行动队列

        // ── 损伤系统 ──
        public Combat.AilmentStacker Ailments = new();

        // ── 延时动作 ──
        private List<DelayedAction> _delayedActions = new();

        // ── 事件 ──
        public event Action<UnitBase> OnUnitDied;
        public event Action<Team> OnGameOver;
        public event Action<UnitBase, Vector2Int> OnUnitMoved;

        // ═══════════════════════════════════════
        //  初始化
        // ═══════════════════════════════════════
        public BattleManager()
        {
            Instance = this;
            Phase = BattlePhase.BanPick;
            GrandRound = 1;
        }

        // ═══════════════════════════════════════
        //  Ban / Pick
        // ═══════════════════════════════════════
        public void ExecuteBan(Character target)
        {
            target.IsBanned = true;
        }

        /// <summary>选将顺位：1-2-2-1-1-2-2-1-1-2</summary>
        public static readonly int[] PickOrder = { 1, 2, 2, 1, 1, 2, 2, 1, 1, 2 };

        public void PickCharacter(Character character, Team pickingTeam)
        {
            if (character.IsBanned || character.IsPicked) return;
            character.IsPicked = true;
            character.Team = pickingTeam;

            if (pickingTeam == Team.Player1)
                Player1Roster.Add(character);
            else
                Player2Roster.Add(character);
        }

        // ═══════════════════════════════════════
        //  布阵（准备阶段）
        // ═══════════════════════════════════════
        public void DeployUnit(UnitBase unit, Vector2Int pos)
        {
            if (!CoordinateSystem.IsValid(pos)) return;
            // 公共区域（x=4）禁止初始选位
            if (pos.x == 4) return;
            if (Board.ContainsKey(pos)) return;

            Board[pos] = unit;
            unit.GridPosition = pos;
        }

        public void EndPreparation()
        {
            Phase = BattlePhase.PlayerTurn;
            DetermineFirstPlayer();
            BuildActingOrder();
            StartGrandRound();
        }

        // ═══════════════════════════════════════
        //  先手判定
        // ═══════════════════════════════════════
        void DetermineFirstPlayer()
        {
            int p1Agility = Player1Roster.Sum(c => c.Agility);
            int p2Agility = Player2Roster.Sum(c => c.Agility);

            if (p1Agility != p2Agility)
            {
                CurrentTurn = p1Agility > p2Agility ? Team.Player1 : Team.Player2;
                return;
            }

            int p1HP = Player1Roster.Sum(c => c.MaxHP);
            int p2HP = Player2Roster.Sum(c => c.MaxHP);
            if (p1HP != p2HP)
            {
                CurrentTurn = p1HP < p2HP ? Team.Player1 : Team.Player2;
                return;
            }

            int p1Atk = Player1Roster.Sum(c => c.BaseAttack);
            int p2Atk = Player2Roster.Sum(c => c.BaseAttack);
            CurrentTurn = p1Atk > p2Atk ? Team.Player1 : Team.Player2;
        }

        void BuildActingOrder()
        {
            // 按敏捷从高到低排列，玩家可自定义内部顺序
            ActingOrder.Clear();
            foreach (var c in Player1Roster) ActingOrder.Add(c);
            foreach (var c in Player2Roster) ActingOrder.Add(c);
            foreach (var s in Summons.Where(s => !s.HasCommand)) ActingOrder.Add(s);
            ActingOrder = ActingOrder.OrderByDescending(u => u.Agility).ToList();
        }

        // ═══════════════════════════════════════
        //  大回合流程
        // ═══════════════════════════════════════
        void StartGrandRound()
        {
            // 所有角色大回合开始 +1 充能
            foreach (var c in Player1Roster.Concat(Player2Roster))
                c.OnGrandRoundStart();

            // 结算流血/燃烧（优先级：先结算伤害与回血——回血先于伤害）
            foreach (var unit in GetAllAliveUnits())
            {
                foreach (var se in unit.StatusEffects.ToList())
                    se.OnGrandRoundStart(unit);
            }

            // 移除过期状态
            foreach (var unit in GetAllAliveUnits())
                unit.StatusEffects.RemoveAll(s => s.IsExpired);

            MiniTurnIndex = 0;
        }

        /// <summary>玩家主动选择某个单位执行小回合</summary>
        public void ExecuteMiniTurn(UnitBase unit)
        {
            if (!unit.IsAlive) return;

            // 眩晕检查
            if (unit.IsStunned)
            {
                EndMiniTurn(unit);
                return;
            }

            // 冷却递减
            foreach (var skill in unit.Skills)
                skill.TickCooldown();

            // 按敏捷从高到低触发自动大招(BD)
            foreach (var skill in unit.Skills.OfType<Skills.UltimateSkill>().Where(s => s.IsAutoCast))
            {
                var character = unit as Character;
                if (character != null && skill.CanUltimate(character))
                    skill.Execute(this, unit, unit.GridPosition);
            }

            // 蓄力打断检查：眩晕/嘲讽打断蓄力
            if (unit.IsCharging && (unit.IsStunned || unit.IsTaunted))
            {
                unit.StatusEffects.RemoveAll(s => s.Tag == StatusTag.Charge);
            }
        }

        public void EndMiniTurn(UnitBase unit)
        {
            // 防反结算
            var counter = unit.StatusEffects.OfType<CounterStatus>().FirstOrDefault();
            if (counter != null)
            {
                foreach (var ret in counter.PendingRetaliations)
                {
                    if (ret.Target != null && ret.Target.IsAlive)
                        ret.Target.TakeDamage(ret.Damage, DamageType.Physical, unit);
                }
                counter.PendingRetaliations.Clear();
            }

            // 状态回合递减
            foreach (var se in unit.StatusEffects)
                se.OnTurnStart(unit);

            unit.StatusEffects.RemoveAll(s => s.IsExpired);

            // 切换下一个单位
            MiniTurnIndex++;
            if (MiniTurnIndex >= ActingOrder.Count)
            {
                // 小回合循环完 → 切换大回合
                SwitchTeam();
            }
        }

        void SwitchTeam()
        {
            CurrentTurn = CurrentTurn == Team.Player1 ? Team.Player2 : Team.Player1;

            if (CurrentTurn == Team.Player1)
            {
                GrandRound++;
                StartGrandRound();
            }
            else
            {
                MiniTurnIndex = 0;
            }

            CheckGameOver();
        }

        /// <summary>跳过当前小回合</summary>
        public void SkipMiniTurn(UnitBase unit)
        {
            EndMiniTurn(unit);
        }

        /// <summary>行动完成（普攻释放后直接结束当前小回合）</summary>
        public void OnActionComplete()
        {
            // 标记当前小回合结束
        }

        // ═══════════════════════════════════════
        //  棋盘操作
        // ═══════════════════════════════════════
        public UnitBase GetUnitAt(Vector2Int pos)
        {
            Board.TryGetValue(pos, out var unit);
            return unit;
        }

        public bool MoveUnit(UnitBase unit, Vector2Int to)
        {
            if (!CoordinateSystem.IsValid(to)) return false;
            if (unit.IsBound) return false;
            if (Board.ContainsKey(to) && Board[to] != null)
            {
                // 召唤物格子：单个格子最多一个召唤物
                if (Board[to] is Summon) return false;
            }

            Board.Remove(unit.GridPosition);
            Board[to] = unit;
            unit.GridPosition = to;
            OnUnitMoved?.Invoke(unit, to);
            return true;
        }

        public void SpawnSummon(Summon summon, Vector2Int pos, Character summoner = null)
        {
            if (!CoordinateSystem.IsValid(pos)) return;
            if (Board.ContainsKey(pos) && Board[pos] != null) return;

            summon.Summoner = summoner;
            Board[pos] = summon;
            Summons.Add(summon);
            if (summon.HasCommand)
            {
                // 指令召唤物归属于召唤师回合，不需要单独行动
            }
            else
            {
                ActingOrder.Add(summon);
            }
        }

        public void RemoveSummon(Summon summon)
        {
            Board.Remove(summon.GridPosition);
            Summons.Remove(summon);
            ActingOrder.Remove(summon);
        }

        // ═══════════════════════════════════════
        //  单位死亡
        // ═══════════════════════════════════════
        public void KillUnit(UnitBase unit)
        {
            Board.Remove(unit.GridPosition);
            if (unit is Character c)
            {
                Player1Roster.Remove(c);
                Player2Roster.Remove(c);
            }
            if (unit is Summon s)
            {
                Summons.Remove(s);
            }
            ActingOrder.Remove(unit);
            OnUnitDied?.Invoke(unit);

            // 延时技能：释放者阵亡则失效
            _delayedActions.RemoveAll(da => da.Owner == unit);

            CheckGameOver();
        }

        // ═══════════════════════════════════════
        //  胜负判定
        // ═══════════════════════════════════════
        public void CheckGameOver()
        {
            bool p1Alive = Player1Roster.Any(c => c.IsAlive);
            bool p2Alive = Player2Roster.Any(c => c.IsAlive);

            if (!p1Alive)
            {
                Phase = BattlePhase.GameOver;
                OnGameOver?.Invoke(Team.Player2);
            }
            else if (!p2Alive)
            {
                Phase = BattlePhase.GameOver;
                OnGameOver?.Invoke(Team.Player1);
            }
        }

        // ═══════════════════════════════════════
        //  延时动作
        // ═══════════════════════════════════════
        public void ScheduleDelayedAction(Action action, int turnsFromNow)
        {
            _delayedActions.Add(new DelayedAction
            {
                Action = action,
                RemainingTurns = turnsFromNow
            });
        }

        public void TickDelayedActions()
        {
            foreach (var da in _delayedActions.ToList())
            {
                da.RemainingTurns--;
                if (da.RemainingTurns <= 0)
                {
                    da.Action?.Invoke();
                    _delayedActions.Remove(da);
                }
            }
        }

        // ═══════════════════════════════════════
        //  辅助
        // ═══════════════════════════════════════
        public IEnumerable<UnitBase> GetAllAliveUnits()
        {
            foreach (var kv in Board)
                if (kv.Value.IsAlive)
                    yield return kv.Value;
        }

        public List<UnitBase> GetEnemiesOf(Team team)
        {
            var enemies = new List<UnitBase>();
            foreach (var kv in Board)
                if (kv.Value.IsAlive && kv.Value.Team != team)
                    enemies.Add(kv.Value);
            return enemies;
        }

        /// <summary>默认优先锁定同列距离最近敌人（存在多个可选目标时可自主选择，嘲讽改变逻辑）</summary>
        public UnitBase GetDefaultTarget(UnitBase attacker)
        {
            // 嘲讽优先
            if (attacker.IsTaunted)
            {
                var tauntSource = attacker.GetTauntSource();
                if (tauntSource != null && tauntSource.IsAlive)
                    return tauntSource;
            }

            // 同列最近敌人
            var enemies = GetEnemiesOf(attacker.Team)
                .Where(e => e.GridPosition.x == attacker.GridPosition.x)
                .OrderBy(e => CoordinateSystem.ManhattanDist(attacker.GridPosition, e.GridPosition))
                .ToList();

            return enemies.FirstOrDefault();
        }

        // ═══════════════════════════════════════
        //  嘲讽攻击逻辑
        // ═══════════════════════════════════════
        public bool CanNormalAttack(UnitBase attacker, UnitBase target)
        {
            if (attacker.IsTaunted)
            {
                var tauntSource = attacker.GetTauntSource();
                // 只能普攻嘲讽来源
                if (target != tauntSource) return false;
            }

            // 隐匿检查
            if (target.IsStealth) return false;

            int dist = CoordinateSystem.ManhattanDist(attacker.GridPosition, target.GridPosition);
            // 查找普攻技能范围
            var basicAttack = attacker.Skills.OfType<Skills.BasicAttack>().FirstOrDefault();
            int range = basicAttack?.Range ?? 1;
            return dist <= range;
        }

        public bool CanTargetWithSkill(UnitBase caster, UnitBase target, Skills.SkillBase skill)
        {
            // 隐匿单位无法被单体技能选中（AOE除外）
            if (target.IsStealth && skill is not Skills.AoeSkill) return false;
            int dist = CoordinateSystem.ManhattanDist(caster.GridPosition, target.GridPosition);
            return dist <= skill.Range;
        }
    }

    public class DelayedAction
    {
        public Action Action;
        public int RemainingTurns;
        public UnitBase Owner; // 延时技能持有者
    }
}