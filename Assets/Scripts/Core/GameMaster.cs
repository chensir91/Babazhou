namespace Babazhou
{
    /// <summary>游戏入口 — Unity MonoBehaviour 挂载到场景 GameObject</summary>
    public class GameMaster : UnityEngine.MonoBehaviour
    {
        public BattleManager Battle;

        void Awake()
        {
            Battle = new BattleManager();

            Battle.OnUnitDied += (unit) =>
            {
                UnityEngine.Debug.Log($"[死亡] {unit.DisplayName} 阵亡");
                Battle.KillUnit(unit);
            };

            Battle.OnGameOver += (winner) =>
            {
                UnityEngine.Debug.Log($"[游戏结束] {winner} 获胜！");
            };

            Battle.OnUnitMoved += (unit, pos) =>
            {
                UnityEngine.Debug.Log($"[移动] {unit.DisplayName} → ({pos.x},{pos.y})");
            };
        }

        /// <summary>示例：快速启动一场测试对战</summary>
        [UnityEngine.ContextMenu("Quick Test Battle")]
        public void QuickTest()
        {
            // 创建测试角色
            var char1 = CreateTestChar("铁甲卫士", 100, 15, 8, Team.Player1);
            var char2 = CreateTestChar("烈焰法师", 80, 20, 10, Team.Player1);
            var char3 = CreateTestChar("暗影刺客", 60, 25, 12, Team.Player2);
            var char4 = CreateTestChar("圣光骑士", 120, 10, 6, Team.Player2);

            Battle.Player1Roster.Add(char1);
            Battle.Player1Roster.Add(char2);
            Battle.Player2Roster.Add(char3);
            Battle.Player2Roster.Add(char4);

            // 布阵
            Battle.DeployUnit(char1, new Vector2Int(1, 2));
            Battle.DeployUnit(char2, new Vector2Int(2, 2));
            Battle.DeployUnit(char3, new Vector2Int(6, 2));
            Battle.DeployUnit(char4, new Vector2Int(7, 2));

            Battle.EndPreparation();

            UnityEngine.Debug.Log($"先手：{Battle.CurrentTurn} | 行动队列：{Battle.ActingOrder.Count} 个单位");
        }

        Character CreateTestChar(string name, int hp, int atk, int agi, Team team)
        {
            var c = new Character
            {
                DisplayName = name,
                MaxHP = hp,
                CurrentHP = hp,
                BaseAttack = atk,
                Agility = agi,
                Team = team
            };

            c.Skills.Add(new Skills.BasicAttack());
            c.Skills.Add(new Skills.ActiveSkill
            {
                SkillName = "重击",
                DamageMultiplier = 1.5f,
                Cooldown = 2,
                Range = 1
            });
            c.Skills.Add(new Skills.UltimateSkill
            {
                SkillName = "终极一击",
                ChargeRequired = 5,
                DamageMultiplier = 2.5f,
                IsAutoCast = true
            });

            return c;
        }
    }
}