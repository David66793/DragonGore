# 开发维护指南

项目总览、打开方式与按键统一见[工作区 README](../../README.md)。以下 Assets、Packages、Logs 等路径均相对于 UnityProject。

## 代码与资源职责

| 目录 / 文件 | 职责与修改入口 |
| --- | --- |
| `Core/GameBalance.asset` | 主要数值，在 Inspector 中调整 |
| `Core/GameInput.cs` | 集中声明按键，通过 Input System 读取键盘 |
| `Core/Health.cs` | 生命、受伤保护、死亡事件 |
| `Core/CameraFollow.cs`、`WorldYSort.cs` | 相机跟随与脚底 Y 排序 |
| `Features/Player/PlayerMotor.cs` | 移动、闪避、位移边界与临时碰撞处理 |
| `Features/Player/PlayerVisualAnimator.cs` | 人物动作与朝向表现 |
| `Features/Combat/PlayerAttack.cs`、`HitRegistry.cs` | 连击、技能与同次攻击伤害去重 |
| `Features/Combat/` 下 VFX / Spark 脚本 | 玩家攻击与命中粒子 |
| `Features/Enemies/EnemyBrain.cs`、`ArrowProjectile.cs` | 敌人状态与箭矢逻辑 |
| `Features/Enemies/` 下 Visual / VFX / Fx 脚本 | 敌人姿态、攻击与受击表现 |
| `Features/Level/LevelDirector.cs`、`LevelRules.cs` | 关卡阶段、救援增援与胜负条件 |
| `UI/GameHud.cs` | 生命与目标信息、开始/暂停/继续/重开/退出菜单 |
| `UI/SkillHud.cs` | 右下角 J/K/L 提示、成功动作反馈、冷却显示；不处理技能输入 |
| `UI/ArtValidationControls.cs` | 美术检查操作 |
| `Scenes/`、`Prefabs/` | 场景与可复用对象 |
| `Art/Characters/ZhaoYun/AnimationSources/` | 各动作版本原图与 SOURCE.md |
| `Art/Characters/ZhaoYun/Animations/` | 生成的图集、动画、材质与 Animator |
| `Editor/` | 仅用于 Unity 编辑器的工具 |
| `Tests/Editor/`、`Tests/PlayMode/` | 逻辑测试与运行测试 |

表内路径均位于 Assets/Game。保持玩法与视觉各自负责，不把移动、攻击、敌人和目标集中进一个脚本。

Changban.Game 是运行时代码程序集；Changban.Editor 仅在编辑器编译；两个测试程序集分别覆盖编辑器与 PlayMode。新增编辑器脚本放 Editor，避免运行时代码引用 UnityEditor。

## 日常修改流程

1. 阅读 [STATUS](STATUS.md)、相关代码与工作区规范，确认当前行为。
2. 修改所属功能目录；主要数值优先调整 GameBalance.asset。连击倍率、增援位置等部分参数仍在脚本中，并非全部数值都已配置化。
3. 等待 Unity 编译，查看 Console；在主场景实际验证所改行为。
4. 战斗或目标规则变化时运行相关测试；视觉变化在 ArtValidation 三种底色及游戏尺寸下检查。
5. 更新 STATUS 的已完成、验证证据、未完成、下一步；验证范围记入 [VALIDATION](VALIDATION.md)。

移动、重命名资源优先在 Unity Project 窗口操作，保持 .meta 的 GUID 稳定。不要手写场景 YAML、猜测 GUID 或重建已有 .meta。模板资源 Assets/Settings、Assets/Scenes、Assets/Welcome 当前保留，清理前先核查引用。

## 初始化与美术接入

**Changban → Setup Demo** 对应 `Changban.EditorTools.DemoSetup.SetupDemo`：补齐缺失基础资源，保留已有场景与预制体，同时配置构建场景列表和原型图导入设置。

**Changban → Apply Art Pass** 对应 `Changban.EditorTools.ArtPassSetup.Apply`：接入原图、生成或更新图集、动画、材质，更新场景与预制体绑定。重复执行用于更新同一套生成内容；会写入已有资源与绑定。先保存场景并备份自定义绑定。日常运行无需重新初始化。

资源检查：`Changban.EditorTools.DemoVerification.ValidateAssets`，检查两张场景与五个预制体的 Missing Script。

**Changban → Apply Animation Polish** 对应 `Changban.EditorTools.AnimationPolishSetup.Apply`：更新接地待机轴点、左右六帧跑动、停步备战状态及检查场景。完整 Apply Art Pass 也会调用此更新，避免恢复旧跑动。保留原图，只写生成资源；旧默认敌人收招 0.8 秒迁移为 0.18 秒，已自定义的值保留。

本轮可调数值位于 GameBalance.asset：`playerIdleDelay`（0.9 秒）、`enemyAttackDuration`（0.16 秒）、`enemyContactTime`（0.07 秒）、`enemyRecovery`（0.18 秒）、`enemyCooldown`（0.60 秒）、`enemyAttackAimDot`（0.25）。敌人接触事件负责一次伤害/箭矢与挥击粒子，视觉从 EnemyBrain 的同一阶段进度取样，不再使用独立攻击时钟。

**Changban → Apply Game UI** 对应 `Changban.EditorTools.GameUiSetup.Apply`：为主场景 GameHud 对象补齐 SkillHud 并绑定 LevelDirector，重复执行不会新增第二个组件或持久化 Canvas。Canvas 在运行时创建并随场景销毁。完整 Art Pass 也调用此入口。

技能输入仍归 PlayerMotor / PlayerAttack；SkillHud 监听 `Dodged`、`SkillUsed`、`AttackStarted` 成功事件。K/L 门槛与 UI 可用状态共用 `CanDodge` / `CanUseSkill`；J 使用 `CanRequestAttack`，排队时不立即闪烁。冷却取游戏时间，暂停不流逝；禁用 UI 时解除订阅，恢复后只订阅一次。图标为 UGUI 网格，程序集引用已有 `Unity.ugui`，没有新增包或字体。

`LevelDirector.RestartGame()` 重载主场景并直接开始；原 `Restart()` 保留回到 Ready 的语义，供已有验证使用。`QuitGame()` 在 Windows 调用 Application.Quit；编辑器通过 `GameUiSetup` 停止 Play，保持编辑器打开。退出请求测试隔离编辑器停止适配器，验证请求事件只发一次。

弓兵 `EnemyAttackVfx.EmitContact` 只发一次少量弓口轻尘。受击火花仍由真实箭碰撞、且 PlayerMotor.ReceiveHit 接受伤害后发射；不得用攻击开始或预警结束直接播放命中特效。

## 测试与构建

打开 **Window → General → Test Runner**，分别运行 EditMode 与 PlayMode 测试。XML 报告导出到 Docs/Reports，截图放 Docs/Screenshots，完整日志放 Logs。

Windows 构建：打开 **File → Build Profiles**，选择 Windows / Windows 64 位，确保启用 Assets/Game/Scenes/ChangbanDemo.unity，输出到 Builds/Windows。ArtValidation 和 SampleScene 不作为游戏入口。自动构建方法：`Changban.EditorTools.DemoVerification.BuildWindows`。

以下 PowerShell 示例从工作区根目录执行，安装路径以本机存在的版本为准。**同一工程已在编辑器打开时，使用当前编辑器，不启动第二个批处理进程。**

```powershell
$unityExe = 'C:\Program Files\Unity\Hub\Editor\6000.6.0f1\Editor\Unity.exe'
$projectPath = (Resolve-Path -LiteralPath '.\UnityProject').Path
$logPath = Join-Path $projectPath 'Logs\asset-validation.log'
$unityProcess = Start-Process -FilePath $unityExe -WindowStyle Hidden -Wait -PassThru -ArgumentList @(
    '-batchmode', '-nographics', '-quit',
    '-projectPath', ('"' + $projectPath + '"'),
    '-executeMethod', 'Changban.EditorTools.DemoVerification.ValidateAssets',
    '-logFile', ('"' + $logPath + '"')
)
if ($unityProcess.ExitCode -ne 0) { throw "Unity 检查失败，请查看 $logPath" }
```

构建时把方法换为 `Changban.EditorTools.DemoVerification.BuildWindows`，日志换为 Logs/windows-build.log。独立 Player 的键盘流程需实际试玩，不以构建成功代替功能验收。

## 文档、日志与版本管理

### 辅助开发工具（2026-09-30）

- **Superpowers 6.4.2 已安装并启用**：从 Codex 的 openai-curated-remote 插件目录安装，标识为 `superpowers@openai-curated-remote`，作者项目为 [obra/superpowers](https://github.com/obra/superpowers)。它是规划、测试、调试、审查与验证的技能插件，不是独立 MCP 服务，不添加 Unity Packages 依赖。安装及启用状态已经 CLI 与插件目录双重核验，本地包含 15 个技能目录。
- 适合后续复杂功能的方案规划、故障定位和完成前验证；简单修改按任务规模使用。用户授权、项目 AGENTS 规则及不自动提交/推送等约束继续优先，不因插件的通用流程重新询问已确认的选择。
- 本轮动作修订中已实际加载并使用 Superpowers 的调试、测试、并行任务和完成前验证技能；其安装与实际使用均有记录。项目协作规则继续优先。
- **CodeGraph 本轮暂不安装**：游戏目录共有 27 个 C# 文件、4,335 行非空代码，现有模块划分与搜索仍可维护。多个同名实现提供 C# 调用关系及影响分析，但所查文档未说明 Unity 场景/预制体的 GUID、序列化绑定和生命周期引用支持；不能用通用调用图代替 Unity 资源检查。
- 当跨模块依赖难以追踪、改动影响范围经常遗漏，或日常代码检索明显变慢时重新评估；届时先确认具体实现，在本项目试验 C# 索引准确性，并排除 Library、Temp、Logs、Builds 和第三方缓存。参考：[codegraph-ai/CodeGraph](https://github.com/codegraph-ai/CodeGraph)、[colbymchenry/codegraph](https://github.com/colbymchenry/codegraph)。

### 归档与备份

- 工作区 Docs：需求、美术与验收规范。初始原图说明属于交接基线，现状以工程 STATUS 为准。
- 工程 Docs：DEVELOPMENT、STATUS、VALIDATION、ART_ASSETS 四类维护文档。
- Docs/Reports：测试 XML，含早期报告与最终 V3 报告，可进入版本管理。
- Docs/Screenshots：真实 Unity 截图，保持已有路径。
- Logs：编辑器、导入、构建与 Player 日志；Logs/Archive 保留从 Docs 迁入的早期日志。不进入 Git，分享验证时按需附上。
- Builds：本机构建产物，不进入 Git。

仓库在 UnityProject/.git，目前尚无提交，不自动提交或推送。保留 Assets 及 .meta、Packages、ProjectSettings、工程文档与测试报告；排除 Library、Temp、Logs、Obj、Builds、UserSettings 与 IDE 缓存。工作区外层文件不在此仓库范围内，备份时也应包括。
