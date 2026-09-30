# 赵云北向步行 V3：素材来源与接入说明

2026-09-29 使用 Codex 内置 `image_gen` 生成，属于本项目新制位图。参考了 `ArtSource/References/ZhaoYun_Character_Reference.png` 的人物身份、银灰甲、灰白袍、青蓝披风、白缨银盔和木杆长枪，以及 `AnimationSources/AttackV2/ZY_N_AttackV2_Windup.png` 的北向斜俯视投影。参考图均未覆盖；未使用外部素材。

| 文件 | 关键姿态 | 实测规格 | Alpha≥64 主体范围（左、上、右、下） | 建议 Sprite pivot（归一化） |
| --- | --- | --- | --- | --- |
| `ZY_N_WalkV3_ContactA.png` | 画面左侧腿后蹬着地，右侧腿向前 | 1254×1254 RGBA | (69, 52, 1235, 1227) | (0.56, 0.022) |
| `ZY_N_WalkV3_Passing.png` | 单脚承重，另一脚抬起过步 | 1254×1254 RGBA | (95, 67, 1197, 1199) | (0.56, 0.044) |
| `ZY_N_WalkV3_ContactB.png` | 画面右侧腿着地，左侧腿向前 | 1254×1254 RGBA | (76, 71, 1211, 1216) | (0.56, 0.030) |

生成提示核心：`rear three-quarter NORTH WALK`, `full-body transparent Unity sprite`, `same Zhao Yun silver-gray armor, white robe, muted teal cape, white plume and diagonal dark-wood spear`, `measured walk, not attack or sprint`, 分别指定左右脚接触与抬脚过步，并要求固定画布比例、脚底落点、真实透明 Alpha、无地面与投影。接触 A 做过一次比例迭代，最终文件为较接近另两帧人物大小的版本。建议循环 `ContactA → Passing → ContactB → Passing`，动画时长和缩放仍需在 Unity 实机调整。

三个文件角落像素均为 Alpha 0，Alpha≥64 的主体均未触碰画布边缘。放大检查可见白缨、披风局部有少量红黄颜色边缘；这是生成素材的可见瑕疵。头盔、双脚与枪杆在原尺寸可辨；**尚未通过游戏内约 160 像素角色高度及浅灰、深灰、灰褐三底色验收**，Unity 接入后应重点检查该边缘、细枪杆和逐帧重心跳动。以上 pivot 按显著像素脚底估算，接触点需以实机画面微调。
