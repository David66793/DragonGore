# 赵云左右跑动 V4（2026-09-30）

`ZY_E_RunV4_Sheet.png` 由内置 ImageGen 生成，透明 RGBA，1536 × 1024，2 列 × 3 行六个向右跑动姿态；向左使用同一循环的水平镜像。身份参考为工作区的赵云设定母版，并参考既有东南待机与步行接地姿态。原图均未覆盖，未导入 Archive。

六帧按接地、过步、腾空、另一侧接地、过步、腾空顺序接入，每帧 0.08 秒，一轮 0.48 秒。`AnimationPolishSetup` 通过 Unity Editor API 创建独立 Sprite、材质、Walk 动画与预制体绑定；不旋转武器图片模拟跑动。图集留白不严格等分，脚底轴点按帧测量校正，所有帧使用相同 Pixels Per Unit，保留过步和腾空的自然高度变化。生成文件的 `.meta` 由 Unity 创建。

此素材是当前 Demo 的动作版本：不是分层骨骼动画，也不是八方向独立动画。透明通道已检查，人物和武器完整性、三底色与连续播放仍按工程验证记录限定验收范围；不能据此声称已通过正式美术验收。

## 生成要求记录

逐帧渲染发现第 3、4 帧下缘会带入下一行羽毛。使用工程已安装的 [Sprite Editor Data Provider API](https://docs.unity3d.com/Packages/com.unity.2d.sprite@1.0/manual/DataProvider.html) 设置切片和自定义轮廓：第 3 帧下缘裁去 14 像素，第 4 帧仅裁下缘中间空隙，保留左右靴子的完整边缘；第 5、6 帧上缘留出羽毛形状，避免截断及带入上一行靴子。源 PNG 不改，脚底 pivot 保持接地位置。

身份保持赵云的年轻面容、银甲、白羽、青蓝披风、长枪，偏写实武侠绘制，匹配已有 2D 伪 3D 视角。修订生成要求将布局改为两列三行，避免第一版三列两行的长枪互相挤占格子。实际使用修订版，第一版未导入工程。

```text
Create a six-frame EAST-facing run cycle sprite sheet for the same Zhao Yun character in the supplied reference images. Preserve the identity, silver layered armor, white helmet plume, teal-blue cape and long spear. Use realistic hand-painted wuxia game art matching the established 2D pseudo-3D elevated view. Face RIGHT in every frame, lean forward naturally and hold the spear stable diagonally forward while the legs alternate through contact, passing and flight poses. The second half-cycle must switch the leading leg. Keep torso size, lighting, face, armor and camera angle consistent; make the leg motion visibly different rather than sliding a single pose.

Use TWO COLUMNS and THREE ROWS, exactly six isolated whole-body frames in reading order. Each cell must contain the entire plume, cape, boots and spear with transparent spacing; no weapon may cross into another cell. Generous horizontal space for the long spear. Transparent background. No ground, cast shadow, glow, labels, text, borders, UI or extra characters. Do not rotate a weapon cutout or change the character into a rider or a 3D model.
```

上述为可复现的生成要求整理；源 PNG 保留生成结果，不对原图做后期覆盖。运行截图与测试见工程 `Docs/VALIDATION.md`。
