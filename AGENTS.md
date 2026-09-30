# 项目协作规则

用中文解释工作，面向 Unity 新手。先读取 CODEX_START_PROMPT.md、Docs/GAME_BRIEF.md、Docs/ART_BIBLE.md、Docs/ACCEPTANCE.md。

用户已确认：Unity 6，三国长坂坡题材，2D 伪 3D，偏写实武侠风，赵云先做步战，不制作任何声音。不得擅自改为纯 3D、骑战、网页游戏或开放世界。

当前目录是包含交接资料与已有 UnityProject 工程的工作区。先识别已有工程的版本和内容，在其基础上继续开发，不覆盖或重新创建。只有工程确实不存在时，才默认创建在 UnityProject 子目录。不要把 ArtSource/Archive 导入项目。

项目总览统一维护根目录 README.md，不另建 UnityProject/README.md。工程维护说明放 UnityProject/Docs/DEVELOPMENT.md，局部素材来源统一用 SOURCE.md；测试报告放 UnityProject/Docs/Reports，日志放 UnityProject/Logs。

保持 Assets/Game 按功能划分。场景和预制体优先通过 Unity Editor API 生成，不凭空编造 YAML、GUID 或 .meta。由 Unity 生成 .meta 后纳入版本管理。编辑器脚本放在 Editor 目录，与运行时代码分开。

固定探测到的 Unity 版本及兼容包版本。读取已存在 manifest 与 packages-lock，不随意升级，不安装大而全框架。只使用一套 2D 物理系统。初期不引入 ECS、复杂依赖注入、联网、Addressables 或多场景流式加载。

输入方案与项目设置必须一致，优先使用模板已有 Input System。依赖缺失时选最小可运行方案并记录，不留下编译错误。没有中文字体时，使用可分发字体或暂用英文 UI 并明确记录，不把方框字视为完成。

玩法逻辑与视觉分开：移动、生命、攻击、敌人状态、关卡目标各有职责。禁止一个 GameManager 包办全部逻辑。数值集中配置。不要用武器图像旋转代替人物全方向动画；只有一个朝向时明确标为占位。

原图不覆盖。角色设定图是身份母版，概念画面是氛围参考，最新姿态只是临时素材。没有动画、分层或透明边缘验收时不得声称已经具备。

每次完成小目标后检查编译和运行日志，能截图则提供真实截图。不能运行时明确写未验证，不伪造测试或性能结论。初始化可重复执行且不会重复生成对象；已有用户场景不破坏。

保留用户更改，不强制关闭编辑器、不删除锁文件、不绕过授权、不自动提交或推送远端。可在新目录初始化本地 Git，排除 Library、Temp、Logs、Obj、Builds，保留 Assets 及 .meta、Packages 和 ProjectSettings。

每个里程碑更新 UnityProject/Docs/STATUS.md：已完成、验证证据、未完成、下一步。完成用户已授权的工作后再汇报，不反复索取常规实现选择的确认。
