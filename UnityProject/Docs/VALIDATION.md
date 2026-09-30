# 验证记录（2026-09-30）

## 当前版本：动作与战斗修订 V4

Unity 6000.6.0f1 实际验证，最终 [PlayMode 报告](Reports/play-polish-v4-release.xml) **32/32**、[EditMode 报告](Reports/edit-polish-v4-release.xml) **3/3** 通过；对应 `../Logs/unity-play-polish-v4-release.log`、`../Logs/unity-edit-polish-v4-release.log`。旧版 [红测报告](Reports/play-polish-v4-red.xml) 与 [禁用特效红测](Reports/play-polish-v4-disabled-red.xml) 保留。

| 项目 | 结果与验证范围 |
| --- | --- |
| 初始待机影子 | 双脚中心与影子距离小于 0.12 世界单位，左右翻转均成立；查看 [右向截图](Screenshots/IdleShadowEastV4.png)、[左向截图](Screenshots/IdleShadowWestV4.png)。 |
| 左右跑动、停步与暂停 | 模拟 A/D 驱动完整运行链路，观察六个不同 Sprite 与左右翻转；短停 0.16 秒保持备战，持续静止后待机；暂停冻结状态、画面与动画进度。查看 [右跑](Screenshots/RunEastV4.png)、[左跑](Screenshots/RunWestV4.png)。 |
| 跑动切片 | Unity 相机逐帧渲染 `Screenshots/ZY_E_RunV4_01.png` 至 `_06.png`，已查看全部六帧；第 3、4 帧不再带入下一行羽毛，第 5、6 帧保留上缘羽毛并排除上一行靴子。原 PNG 不覆盖。 |
| 第三段 J 粒子 | 实际排队连击到第三段有效帧，空挥也发射，前两段与第三段前摇不发；有限寿命、正常收招余辉、暂停、K/死亡/禁用取消及四向世界坐标定位通过。查看 [第三段截图](Screenshots/Combo3AttackCloseup.png)。 |
| 三类敌人攻击 | 10 项测试验证稳定预警、延迟接触、每次近战/精英一次扣血（去除玩家受击保护验证）、弓兵一箭、每次一次挥击粒子、锁定方向与绕背、短收招后待机冷却、暂停和死亡取消尚未触发的接触。查看 [预警](Screenshots/EnemyWarningParticles.png)、[挥击](Screenshots/EnemyAttackParticles.png)。 |
| 重复初始化与资源 | `../Logs/unity-polish-v4-release.log`：连续执行更新，生成资源 SHA256 不变，Animator 状态无重名，六张独立跑动 Sprite；两个场景、五个预制体 0 Missing Script。 |
| Windows 构建 | 同一日志：Succeeded、0 errors、Unity 正常退出 0；更新 `../Builds/Windows/ChangbanDemo.exe` 与同级 Data。 |
| 独立程序启动 | `../Logs/player-polish-v4-smoke.log`：约 8 秒启动观察，Unity 6 / Input System / Mono / D3D12 初始化，未见脚本异常；主动结束仅本次创建的后台 Player，进程退出码不作成功证据。 |
| 原图与依赖 | 原始 5 张图片 SHA256 与 ASSET_MANIFEST 一致；沿用已有锁定依赖，编辑器程序集仅增加对已安装 Sprite Editor 的引用。 |

以下历史截图路径会由同名测试刷新，因此查看时以当前版本为准，旧版本的测试报告和日志仍保留。**自动测试与相机截图不代替完整人工键盘试玩、正式美术验收或性能结论。**

## 目录整理检查

Unity 6000.6.0f1 批处理实际打开工程，执行 `Changban.EditorTools.DemoVerification.ValidateAssets`，退出码 0，未见 C# 编译错误；两个场景及五个预制体均为 0 Missing Script。日志：`../Logs/unity-structure-validation.log`。

README 唯一、本地文档链接有效；历史日志与报告迁移、原图、来源说明及 .meta 校验通过，日志：`../Logs/structure-static-validation.log`。未重新执行完整游戏测试、构建或人工试玩。

## 整理前的玩法与构建验证

| 项目 | 结果 | 证据与范围 |
| --- | --- | --- |
| Unity 版本与 C# 编译 | 通过 | Unity 6000.6.0f1；最终 PlayMode 与 Windows 构建无 C# error。 |
| 地图覆盖与玩家边界 | 通过 | 最终 PlayMode 覆盖连续地面、可见栅栏、相机边缘及玩家位置约束。 |
| K 闪避 | 通过 | 穿过敌人时无敌人物理位移、无攻击伤害；玩家保持实体障碍碰撞，离开敌人后恢复接触；出现扬尘粒子。 |
| J 三段连击 | 通过 | 三次排队输入分别播放 Attack、Attack2、Attack3；三段攻击帧不同，目标每段只受伤一次，第 3 段增加 1 点伤害。截图见 `Screenshots/Combo2AttackCloseup.png`、`Combo3AttackCloseup.png`。 |
| 上下步行 | 通过，范围有限 | Animator 直接取样验证北、南步行动画的独立帧和材质，并渲染 `WalkNorthCloseup.png`、`WalkSouthCloseup.png`；无头测试不代替真实持续按键手感。 |
| 主角受击 | 通过 | 血量减少、短时闪红、火花与冲击环出现，受击保护阻止连续重复伤害。截图 `Screenshots/PlayerHurtParticles.png`。 |
| 敌人攻击动作与特效 | 通过 | 三类敌人均出现粒子预警、出手姿态和攻击粒子；弓兵出现箭矢和粒子尾迹。旧方向光条、攻击光条及箭尾贴图不显示。 |
| 原有玩法回归 | 通过 | [PlayMode 报告](Reports/play-v3-final-results.xml)：**14/14**；[EditMode 报告](Reports/edit-v3-final-results.xml)：**3/3**。原始 Logs 报告仍保留。 |
| 场景与预制体资源 | 通过 | `../Logs/unity-v3-final-assets.log`：两张场景、五个预制体 0 Missing Script。 |
| Windows 64 位构建 | 通过 | `../Logs/unity-v3-final-build.log`：Succeeded，0 errors；可执行文件在 `../Builds/Windows/ChangbanDemo.exe`，保留同级 Data 目录。 |
| 独立 Player 启动 | 通过 | `../Logs/player-v3-final-smoke.log`：Unity 6000.6.0f1、Input System、Mono 与 D3D12 初始化，短时启动未见脚本异常；隐藏测试进程已结束。 |
| 完整人工试玩、性能及设备兼容 | 未验证 | 尚需人工完成全部按键和胜负流程，并持续运行与测帧率。 |

本轮截图：[第 2 段攻击](Screenshots/Combo2AttackCloseup.png)、[第 3 段攻击](Screenshots/Combo3AttackCloseup.png)、[北向步行](Screenshots/WalkNorthCloseup.png)、[南向步行](Screenshots/WalkSouthCloseup.png)、[敌人粒子预警](Screenshots/EnemyWarningParticles.png)、[敌人粒子攻击](Screenshots/EnemyAttackParticles.png)、[主角受击](Screenshots/PlayerHurtParticles.png)。
