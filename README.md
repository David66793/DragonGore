# 三国：龙胆归戈

Unity 6 长坂坡赵云步战 Demo：2D 伪 3D、偏写实武侠风、单人 Windows 键鼠操作，无音乐、音效或语音。当前已有 Unity 工程、关卡闭环、动作关键帧和 Windows 构建；根目录是工作区入口。

## 打开与运行

1. 在 Unity Hub 中「添加项目」，选择本目录下的 **UnityProject**，而不是工作区根目录。
2. 使用 **Unity 6000.6.0f1** 打开，等待导入与脚本编译完成。
3. 打开 `Assets/Game/Scenes/ChangbanDemo.unity`，点击 **Play**，按 **Enter / Space** 或画面中的 **Begin** 开始。
4. 已有 Windows 版本在 `UnityProject/Builds/Windows/ChangbanDemo.exe`。复制时保留同级 Data 目录和其他构建文件。

| 按键 | 操作 |
| --- | --- |
| WASD | 八向移动 |
| J 连按三次 | 直刺 → 挑扫 → 低身平刺 |
| K | 闪避，短暂无敌、可穿过敌人，本身不造成伤害 |
| L | 破阵突刺，5 秒冷却 |
| E | 靠近阿斗后救援 |
| Esc | 暂停 / 继续 |
| R | 战斗中、暂停时或胜负后重新开始 |

右上角 **Pause / Esc** 打开暂停菜单：**Resume** 继续、**Restart** 重新开始、**Quit Game** 退出。重新开始会恢复生命、敌人、目标与技能冷却，并直接进入新一局；编辑器内退出只停止 Play，Windows 版退出会关闭游戏。

右下角 J/K/L 技能栏分别显示连击、闪避与破阵突刺。动作成功执行时短暂亮起；K/L 冷却期间图标变暗、显示倒计时，重复按键不会执行或再次亮起。J 的排队输入在下一段攻击实际开始时反馈。

胜利顺序：**救援阿斗 → 击败出口精英 → 到达撤离路标**。

美术检查场景：`Assets/Game/Scenes/Tests/ArtValidation.unity`。Play 后按 1/2/3 切换底色，4/5/6 切换显示高度，7/8 翻看姿态。

## 目录与文档入口

```text
三国：龙胆归戈/
├─ README.md                    唯一项目总览与启动说明
├─ AGENTS.md                    开发协作规则
├─ CODEX_START_PROMPT.md         首轮建设任务及继续开发说明
├─ ASSET_MANIFEST.json           原始交接图片清单与校验值
├─ Docs/                        需求、美术规范、验收与原图导入建议
├─ ArtSource/                   身份母版、概念参考、静态原图、历史归档
└─ UnityProject/                Unity Hub 应打开的工程；本地 Git 仓库
   ├─ Assets/Game/              游戏代码与资源
   │  ├─ Core/                 生命、输入、数值、相机、脚底排序
   │  ├─ Features/             Player / Combat / Enemies / Level
   │  ├─ UI/                   HUD 与美术检查界面
   │  ├─ Scenes/               主场景；Tests/ 内为美术检查场景
   │  ├─ Prefabs/              玩家、敌人、箭矢预制体
   │  ├─ Art/                  角色、敌人、环境、特效与生成资源
   │  ├─ Editor/               初始化、美术接入、资源检查与构建工具
   │  └─ Tests/                Editor 与 PlayMode 测试
   ├─ Assets/Settings/          模板的 URP 2D 与输入资源
   ├─ Assets/Scenes、Welcome/   保留的模板场景与欢迎资源
   ├─ Packages/                依赖清单与锁定版本
   ├─ ProjectSettings/         Unity 项目设置
   ├─ Docs/                    开发指南、状态、美术来源、验证记录
   │  ├─ Reports/              历史与最终测试 XML 报告
   │  └─ Screenshots/          已有 Unity 相机截图
   ├─ Logs/                    本地验证日志；Archive/ 为早期日志
   ├─ Builds/                  本地 Windows 构建输出
   └─ Library、UserSettings/   自动生成的缓存与个人设置
```

| 需要了解什么 | 阅读入口 |
| --- | --- |
| 修改代码、数值、资源、测试与构建 | [开发维护指南](UnityProject/Docs/DEVELOPMENT.md) |
| 已完成、验证证据、限制与下一步 | [当前状态](UnityProject/Docs/STATUS.md) |
| 已有测试、截图与构建的验证范围 | [验证记录](UnityProject/Docs/VALIDATION.md) |
| 当前美术的来源、接入与限制 | [美术资源说明](UnityProject/Docs/ART_ASSETS.md) |
| 游戏需求与范围 | [GAME_BRIEF](Docs/GAME_BRIEF.md) |
| 身份母版、风格与视觉约束 | [ART_BIBLE](Docs/ART_BIBLE.md) |
| 验收标准 | [ACCEPTANCE](Docs/ACCEPTANCE.md) |
| 初始静态原图的导入建议 | [SPRITE_IMPORT](Docs/SPRITE_IMPORT.md) |
| 原始交接来源 | [SOURCES](Docs/SOURCES.md) |

工作区 `Docs/` 保存设计规范，`UnityProject/Docs/` 保存工程实施与验证记录。当前进度统一在 STATUS 中维护。

## 固定环境与开发入口

现有配置：Unity **6000.6.0f1**、URP **17.6.0**、Input System **1.19.0**、Test Framework **1.8.0**。输入设置为 Input System，二维碰撞使用 Physics2D。Unity MCP 已作为固定 Git 提交依赖保留；实时 Bridge 会话尚未验证。后续沿用 manifest 与 packages-lock，不自行升级。

- 主要数值：`Assets/Game/Core/GameBalance.asset`。
- 键位：`Assets/Game/Core/GameInput.cs`；当前玩法直接读取 Input System 键盘状态。
- 补齐缺失基础场景或预制体：**Changban → Setup Demo**。
- 更新生成图集、动画与美术绑定：**Changban → Apply Art Pass**。会更新已有生成资源与绑定，先备份自定义修改；日常打开游戏无需执行。
- 单独更新本轮跑动、影子与备战状态：**Changban → Apply Animation Polish**。已接入当前工程，日常运行无需再次执行。
- 补齐主场景技能栏：**Changban → Apply Game UI**，重复执行不会生成多个技能栏。已接入当前工程与完整 Art Pass。
- 测试、批处理方法与 Windows 构建步骤见[开发维护指南](UnityProject/Docs/DEVELOPMENT.md)。

移动 Unity 资源时在 Project 窗口操作，保留 `.meta`。身份母版不得覆盖，`ArtSource/Archive` 不导入工程。素材局部来源统一记录在 `SOURCE.md`。

## 当前完成度与验证边界

已有移动、三段攻击、闪避、突进、敌兵与精英、救援、增援、胜负、暂停和重开。赵云左右跑动使用新六帧循环，上下沿用北向 3 帧、南向 4 帧循环；停步后先保持备战姿态，静止 0.9 秒才待机。第三段 J 增加短银白气流与闪屑。敌人的伤害、箭矢和挥击粒子共用一次接触事件，预警保持稳定，出手与收招更加紧凑。

弓兵空射仅有弓口轻尘和箭尾迹，实际命中且造成伤害才播放受击火花、冲击环与闪红。暂停菜单、退出及右下角技能栏已接入；界面沿用英文，避免缺少中文字体造成方框字。

当前动作修订的测试、真实截图、资源检查与 Windows 构建记录见[验证记录](UnityProject/Docs/VALIDATION.md)，最新完成范围统一记在[状态](UnityProject/Docs/STATUS.md)。历史报告保留，便于追踪改动。

当前仍是验证 Demo：斜向复用相近四向姿态；闪避、受击、死亡主要使用东南向素材；敌兵以关键姿态切换表现动作。地面有轻微纹理重复，UI 暂用英文。完整人工试玩、正式美术验收与长时间性能测量尚未完成。

本地 Git 仓库位于 `UnityProject/`，尚无提交，仅覆盖工程目录；根目录 README、规范和 ArtSource 需随完整工作区一起备份。缓存、日志与构建输出不提交，Assets 及 `.meta`、Packages、ProjectSettings、工程文档和测试报告应保留。
