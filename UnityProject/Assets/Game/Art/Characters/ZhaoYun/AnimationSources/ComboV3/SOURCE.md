# 赵云连击第 2、3 段关键帧

- 生成方式：Codex 内置 `imagegen`，2026-09-29；四张 PNG 为新生成资源，原有 AttackV2 和身份母版未改动。
- 身份参考：`ArtSource/References/ZhaoYun_Character_Reference.png`；画风、镜头、比例参考：`AnimationSources/AttackV2/ZY_SE_AttackV2_Coil.png` 与 `ZY_SE_AttackV2_Extend.png`。
- 主提示词：固定东南朝向的 2D 斜俯视写实武侠赵云，银灰甲、白袍、低饱和青蓝披风、银盔白缨、深木枪杆；全身和整杆枪位于 1536×1024 透明画布内；脚底稳定；枪尖瞄向敌人上身；无背景、地面阴影、光柱、粒子、文字和多余肢体。
- 第 2 段：`ZY_SE_Combo2_Windup.png` 收身拧腰、枪斜持在身前；`ZY_SE_Combo2_Sweep.png` 向右上横扫挑枪。与第 1 段平直刺击形成不同剪影。
- 第 3 段：`ZY_SE_Combo3_Windup.png` 低身平枪蓄力；`ZY_SE_Combo3_Lunge.png` 大跨步贯刺。枪尖始终在敌人身体高度。

## 导入锚点

四帧均为 1536×1024 RGBA，背景四角 Alpha 为 0。按 Alpha ≥128 的脚底位置测量，图像 Y 坐标自上向下：

| 帧 | 脚底 Y | 建议 Sprite pivot Y（自底归一化） |
| --- | ---: | ---: |
| Combo2_Windup | 984 | 0.039 |
| Combo2_Sweep | 980 | 0.043 |
| Combo3_Windup | 944 | 0.078 |
| Combo3_Lunge | 879 | 0.142 |

建议将四帧脚底对齐到相同世界 Y。第 3 段长跨步让头部下降，不能用统一画布中心锚点，否则看起来会漂浮。当前仅有东南向关键姿势，其余方向需要后续独立绘制；四帧并非完整逐帧动画。
