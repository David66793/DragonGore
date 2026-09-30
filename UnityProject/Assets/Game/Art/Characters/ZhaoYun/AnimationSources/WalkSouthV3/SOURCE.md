# 赵云南向步行动作源图 V3

这组素材由内置 ImageGen 生成，参考 `ArtSource/References/ZhaoYun_Character_Reference.png` 的身份、服装与配色，以及 `AnimationSources/AttackV2/ZY_S_AttackV2_Recovery.png` 的游戏视角和画法。参考图和旧动作均未改动。

## 帧顺序

1. `ZY_S_WalkV3_ContactLeft.png`：画面左侧的脚向前接地。
2. `ZY_S_WalkV3_Pass.png`：画面右侧的脚抬起过步。
3. `ZY_S_WalkV3_ContactRight.png`：画面右侧的脚向前接地。
4. `ZY_S_WalkV3_PassOther.png`：画面左侧的脚抬起过步。

建议按上列顺序循环，每帧约 0.10～0.14 秒；可先用 0.12 秒试听视觉节奏，再在 Unity 中根据实际速度调整。所有帧均为 1536 × 1024 RGBA。建议统一 Sprite pivot 为 `(0.50, 0.03)`，即人物双脚接地线约为源图 y=993～1007（从上往下计数）。动画播放期间不要逐帧改人物世界位置；位移交给 `PlayerMotor`。

## 验收边界

四张图具有不同的腿部与重心姿态，枪保持双手横持，人物朝向画面下方。已逐张目视检查全身和枪头不裁切、画布尺寸、透明角点与接地线。低透明度软边仍需在 Unity 的浅灰、深灰、灰褐底色及实际缩小尺寸下检查；尚未完成 Unity 实机播放和逐帧配准验收。手绘关键帧之间仍可能存在面部、纹饰及披风形状轻微漂移，不能视为骨骼一致性已达正式成品水平。
