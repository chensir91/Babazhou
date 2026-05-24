# 《八宝舟》— Unity 项目说明

## 项目结构

```
Babazhou/Assets/Scripts/
├── Core/
│   ├── Enums.cs              # 所有枚举定义
│   ├── BattleManager.cs      # 核心战斗管理器（回合/棋盘/胜负）
│   └── GameMaster.cs         # Unity 入口 MonoBehaviour
├── Units/
│   ├── UnitBase.cs           # 单位基类（HP/攻击/状态）
│   ├── Character.cs          # 角色（充能/BanPick）
│   └── Summon.cs             # 召唤物
├── Skills/
│   └── SkillBase.cs          # 技能体系（普攻/主动/AOE/大招/被动/冲锋/贯穿）
├── StatusEffects/
│   └── StatusEffect.cs       # 7种战斗状态 + 防反
├── Combat/
│   └── DamageSystem.cs       # 伤害计算 + 损伤叠层
└── Utils/
    ├── CoordinateSystem.cs   # 7×5棋盘坐标
    ├── Vector2Int.cs         # 独立坐标结构体
    └── Mathf.cs              # 数学工具
```

## 规则覆盖率

| 系统 | 实现状态 |
|------|---------|
| 7×5棋盘 + 坐标 | ✅ |
| 禁选4名角色 | ✅ |
| 1-2-2-1 选将 | ✅ |
| 先手判定（敏捷→生命→攻击） | ✅ |
| 小回合行动 + 跳过回合 | ✅ |
| 普攻/主动(冷却)/大招(充能) | ✅ |
| 自动大招(BD) | ✅ |
| 嘲讽/隐匿/眩晕/束缚/蓄力/流血/护甲 | ✅ |
| 防反系统 | ✅ |
| 贯穿伤害 / 穿透伤害 | ✅ |
| 冲锋机制 | ✅ |
| 损伤叠层(神经/恐惧/燃烧) | ✅ |
| 延时技能(释放者阵亡失效) | ✅ |
| 群体伤害按敏捷结算 | ✅ |
| 召唤物(指令/独立) | ✅ |
| 全部队消灭判定 | ✅ |

## 在 Unity 中运行

1. 打开 Unity Hub → 新建 2D 项目，命名为 `Babazhou`
2. 将本目录下 `Assets/` 中的所有 `.cs` 文件复制到 Unity 项目的 `Assets/Scripts/` 中
3. 在场景中创建空 GameObject，挂载 `GameMaster.cs`
4. 运行 → 右键 `GameMaster` 组件 → `Quick Test Battle`

## 后续开发路线

| 优先级 | 任务 |
|--------|------|
| P0 | 挂载 Unity MonoBehaviour（目前是纯逻辑层，需绑定 UI） |
| P0 | 棋盘可视化：Grid 渲染 + 单位 Sprite + 移动动画 |
| P1 | UI 系统：HP 条、技能按钮、回合指示器 |
| P1 | 选将界面：Ban/Pick 交互 |
| P2 | 数据驱动：JSON 配置角色/技能 |
| P2 | 网络对战：P2P 或权威服务器 |
| P3 | AI 对手：MiniMax + 启发式评估 |
| P3 | 音效、特效、美术资源 |