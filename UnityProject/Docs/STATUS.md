# 项目状态（2026-09-30）

## 最新里程碑：影子、跑动与战斗表现修订 V4

### 已完成

- 初始待机的脚底轴点调整到双脚之间，左右翻转后影子仍对齐；使用新的 Sprite 资产，原图及旧图集保留。
- 重做左右六帧跑动，向左使用镜像，0.48 秒一轮。所有帧统一缩放，逐帧对齐脚底；移除跑动残影与明显摇摆。停步先保持对应朝向的备战姿态，静止 0.9 秒才待机；暂停保持当前跑姿。
- 第三段 J 在有效出枪帧增加独立银灰气流、银白闪屑和脚尘，空挥也可见，约 0.26–0.38 秒淡出。正常收招保留短余辉，闪避、L、死亡、禁用特效取消；不与 L 混用粒子系统。
- 敌人预警改为一次发射并持续可见；锁定出手方向。攻击状态 0.16 秒，在第 0.07 秒统一触发一次伤害/箭矢/挥击粒子。收招缩短至 0.18 秒，随后 0.60 秒待机冷却；保留可躲避的前摇。近战增加方向检查，绕到背后不会被本次正面挥击打中。
- 动作、伤害与特效共用 EnemyBrain 阶段时钟，消除旧 VFX 时钟不同步和反复预警闪烁。旧代码原本每次挥击只扣一次血，本次修复的是重复提示、接触时机、方向与收招表现。
- 新增 Apply Animation Polish 更新入口，并接入完整 Art Pass。Sprite Editor API 修正两行之间的羽毛/靴子串入；重复执行保持资源稳定，不追加重复姿态或 Animator 状态。参数入口与来源说明已更新。
- Windows 版已重新构建：`Builds/Windows/ChangbanDemo.exe`。修改前的生成资源与代码备份在 `Logs/Backups/AnimationPolishV4`；未提交或推送 Git。

### 验证证据

- Unity **6000.6.0f1** 实际编译、渲染与测试。最终 [PlayMode 报告](Reports/play-polish-v4-release.xml)：**32/32 通过**；[EditMode 报告](Reports/edit-polish-v4-release.xml)：**3/3 通过**。日志分别为 `Logs/unity-play-polish-v4-release.log`、`Logs/unity-edit-polish-v4-release.log`。
- [旧版回归红测](Reports/play-polish-v4-red.xml)：15 项中 12 项失败；[禁用特效红测](Reports/play-polish-v4-disabled-red.xml) 实际复现禁用后错误发射 21 枚粒子，修复后通过。
- 跑动验证通过 Input System 模拟真实 A/D，观察全部六帧、左右镜像、停止后的备战/待机延迟、暂停保持。逐帧 PNG 是 Unity 相机直接渲染的姿态检查；并非人工键盘试玩或合成概念图。
- 真实截图：`Docs/Screenshots/IdleShadowEastV4.png`、`IdleShadowWestV4.png`、`RunEastV4.png`、`RunWestV4.png`、`ZY_E_RunV4_01.png` 至 `_06.png`、`Combo3AttackCloseup.png`、`EnemyWarningParticles.png`、`EnemyAttackParticles.png`。已查看修正后的六帧及攻击截图，未见此前串入的羽毛碎片。
- `Logs/unity-polish-v4-release.log`：重复更新后生成资源 SHA256 保持一致，Animator 状态无重名，六张独立跑动 Sprite；两场景、五预制体 **0 Missing Script**；Windows 构建 **Succeeded，0 errors**，正常退出 0。
- `Logs/player-polish-v4-smoke.log`：新独立 Player 完成 Unity 6、Input System、Mono、D3D12 初始化，约 8 秒启动观察未见脚本异常。后台测试创建的 Player 已结束，不将主动结束的进程退出码当作正常退出证据。
- `Logs/polish-v4-static-validation.log`：原始交接 5 张图片 SHA256 与 ASSET_MANIFEST 一致；Packages / ProjectSettings 的 31 个文件与整理前校验值一致；README 唯一，更新文档链接有效，新资源 `.meta` 齐全。Superpowers 技能已实际加载并用于本轮调试、测试、任务分工与审查。

### 未完成

- 左右跑动仍为六帧图像循环，向左镜像；敌人沿用现有关键姿态并优化时序，不是新增逐帧骨骼动画。斜向仍复用相近主要方向。
- 连续人工键盘手感、完整胜负流程、正式身份/透明边缘美术验收和长时间性能未在本轮完成。当前新素材不是正式八方向动画交付。

### 下一步

1. 直接打开更新后的 Windows Demo，检查左右跑动与停步，J 连按三次观察最后一击，靠近敌人检查预警、出手和绕背躲避。
2. 按实际手感反馈继续微调 GameBalance 中的待机延迟、出手接触、收招与冷却；后续补斜向动作及正式美术验收。

## 上次里程碑：开发工具评估与接入

### 已完成

- 统计 Assets/Game：27 个 C# 文件、4,335 行非空代码。现阶段尚不需要额外代码图谱服务；CodeGraph 暂不安装，具体理由与重新评估条件记入 [DEVELOPMENT](DEVELOPMENT.md)。
- 按用户条件授权安装 Superpowers 6.4.2：来源 Codex 插件目录，标识 `superpowers@openai-curated-remote`。它提供开发流程技能，并非 MCP 服务。

### 验证证据

- `codex plugin add superpowers@openai-curated-remote --json` 成功；`codex plugin list --json` 核验 installed=true、enabled=true、version=6.4.2。
- 插件目录查询亦返回 installed=true；本地 plugin.json 与 15 个技能目录存在。未修改 Unity 脚本、场景、资源或包依赖，本次未重跑 Unity 编译与游戏测试。

### 未完成

- 安装时尚未验证技能实际加载与调用；现已在上方 V4 动作修订任务中验证。
- CodeGraph 的具体同名实现、Unity 引用支持与本项目索引准确性尚未做试验，因此没有安装或声明已接入。

### 下一步

1. Superpowers 的实际技能加载已由上方 V4 任务验证，继续按项目规则使用。
2. 随项目依赖复杂度增长，再评估适合 C# 与 Unity 的代码索引工具。

## 此前里程碑：结构与文档整理

### 已完成

- 统一根目录 README.md 为唯一项目总览，合并原工程说明，更新打开方式、键位、完成度、版本、目录与开发入口；删除 UnityProject/README.md。
- 新增 [DEVELOPMENT](DEVELOPMENT.md)，说明模块职责、数值与键位入口、初始化写入范围、测试、构建及备份方式。工作区 Docs 保留设计规范，工程 Docs 保留实施记录。
- 将工程 Docs 中 26 份旧日志迁入 Logs/Archive，8 份测试 XML 迁入 Docs/Reports；复制两份最终 V3 报告至 Reports，保留 Logs 原报告。迁移内容逐项 SHA256 校验一致。
- 南向步行动作目录 README.md 改名 SOURCE.md，同步保留既有 .meta，内容与 GUID 不变。
- 修正 AGENTS 与首轮建设指令的过时状态、README 路径，标明早期美术文档属于交接原图基线。

### 验证证据

- `Logs/unity-structure-validation.log`：Unity 6000.6.0f1 实际打开工程并执行 DemoVerification.ValidateAssets，正常退出（0）；未见 C# 编译错误，两个场景、五个预制体均为 0 Missing Script。最初沙箱启动未获取授权，使用正常本机权限重试成功，没有更改授权。
- `Logs/structure-static-validation.log`：项目 README 唯一，本地 Markdown 链接有效；5 张原始交接图片与 ASSET_MANIFEST SHA256 匹配。
- `Logs/structure-reorganization.json`：记录迁移路径与 SHA256；8 份报告、26 份日志、来源说明及 meta 校验一致。
- `Logs/structure-protected-files.json`：Unity 检查前记录 447 个 Assets、Packages、ProjectSettings 和 ArtSource 文件；检查后无内容变化。本次未修改玩法脚本、场景、预制体或依赖版本。

### 未完成

- 本次为文档与文件分类整理，没有重新运行全套 PlayMode / EditMode 测试、Windows 构建或完整键盘试玩。下方游戏验证结论来自整理前记录。
- 原有关卡手感、完整人工试玩、正式美术验收与性能测量仍需后续验证。

### 下一步

1. 在 UnityProject 主场景完整试玩现有流程，记录具体问题。
2. 按后续用户任务在对应 Features 模块继续开发，修改后更新本文件与验证记录。

以下保留整理前的玩法状态与验证证据。

## 已完成

- Unity 6000.6.0f1 的 2D 伪 3D 长坂坡步战 Demo 保持原有关卡闭环：赵云救援阿斗、击败精英并撤离；无声音。
- **K 是闪避，不是铁山靠。** K 本身不造成伤害。闪避和 L 突进期间只临时忽略玩家与敌人的物理碰撞，避免高速位移把敌人推飞；地图障碍仍保持实体碰撞。闪避有扬尘和气流，HUD 明确写出无攻击伤害及短暂无敌。
- **J 为按三次的连击。** 第 1 段直刺、第 2 段挑扫、第 3 段低身平刺；后续输入可在当前段期间排队，收招后还保留 0.38 秒衔接窗口。第 2、3 段有独立伤害判定、前踏距离、粒子轨迹和打中反馈。东/西、北、南均制作了对应新关键帧和 Animator 状态。
- 北向步行新增 3 帧、南向步行新增 4 帧双脚交替的独立循环；移动时按方向切换，不再拿待机图做上下移动。美术源图保留于 `Assets/Game/Art/Characters/ZhaoYun/AnimationSources` 各 V3 目录；图集、动画、材质及预制体绑定由 Unity Editor API 生成。
- 敌人的攻击预警、出手、命中和箭矢尾迹改用粒子。旧的连续方向光线、攻击光条、箭尾贴图和发光预警圆盘不再显示。最后一次修改把预警粒子从沿途点列改为武器口与落点两个短暂粒子簇。
- 主角受击有暖红火花、短促冲击环、角色红色闪色和屏幕边缘闪色。旧方向占位小条已通过 Unity Editor API 在玩家预制体中关闭。

## 验证证据

- `Logs/unity-v3-final-artpass.log`：新图集、动画和预制体更新完成，未见脚本编译错误；Unity 自动生成相应 `.meta`。
- `Logs/play-v3-final-results.xml`：**PlayMode 14/14 通过**，覆盖 K 穿敌不推飞/不伤害并恢复碰撞、J 三段独立姿态和逐段伤害、受击闪色与粒子、三类敌人全粒子攻击、L 技能，以及既有地图和关卡测试。北、南步行项核查 Animator 帧与材质的实际渲染；完整键盘手感仍须实际试玩。
- `Logs/edit-v3-final-results.xml`：**EditMode 3/3 通过**。`Logs/unity-v3-final-assets.log`：两个场景、五个预制体均为 0 Missing Script。
- `Docs/Screenshots/Combo2AttackCloseup.png`、`Combo3AttackCloseup.png`、`WalkNorthCloseup.png`、`WalkSouthCloseup.png`、`EnemyAttackParticles.png`、`EnemyWarningParticles.png`、`PlayerHurtParticles.png` 已在最终版本重新由 Unity 相机渲染并检查。旧方向条消失，敌方预警为两簇粒子，无连续光柱。
- `Logs/unity-v3-final-build.log`：Windows 64 位构建 **Succeeded，0 errors**，交付入口为 `Builds/Windows/ChangbanDemo.exe`。同级 Data 文件已更新；exe 的 Unity 启动器自身时间戳可能保持原日期。
- `Logs/player-v3-final-smoke.log`：新构建独立 Player 完成 Unity 6、Input System、Mono 与 Direct3D 12 初始化，短时启动未见脚本异常。测试进程已结束；这不替代完整键盘试玩。

## 尚待验证与限制

- 这是最小 Demo，斜向仍使用相近的东/西、北、南关键帧，并非八方向逐帧动画。步行和连击已改善四向主要动作，但独立 Windows 版完整键盘试玩及长时间性能尚未验证。
- UI 暂用英文，因为工程还没有可分发中文字体。没有声音。

## 下一步

1. 在 Windows Demo 中完整试玩：WASD 移动、J 按三次连击、K 闪避、L 破阵突刺、E 救援、Esc 暂停和 R 重开。
2. 根据实际手感继续补足斜向逐帧动画，并进行持续运行与性能测量。
