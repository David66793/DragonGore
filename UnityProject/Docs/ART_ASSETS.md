# 本轮美术资源说明

原始角色设定图 `ArtSource/References/ZhaoYun_Character_Reference.png` 只作为赵云身份母版；`ArtSource/References/Changban_Target_Gameplay_Concept.png` 只作为氛围参考。两者与旧姿态均未覆盖，也未将 `ArtSource/Archive` 导入 Unity。

V5 技能栏参考概念图右下角的三个方形按键位：深色底、金色边框、银色长枪/闪避/多枪图标，J/K/L 与英文动作名。图标通过 UGUI 网格绘制，没有裁剪概念原图或新增位图素材。沿用可分发的 Unity 内置 LegacyRuntime 字体，暂用英文；中文界面尚待字体接入。弓兵发射改为低亮度弓口轻尘，命中形状火花只保留在实际造成伤害时。

## 生成提示的关键约束

- **赵云**：以身份母版和上一姿态保持盔甲、灰白衣袍、深灰青披风、白缨及长枪一致；东南向、斜俯视、偏写实武侠、完整双脚、真实透明背景。分别生成待机、行走接触与过步、直刺准备/刺出/收招、闪避、突进、受击、倒地。
- **敌兵**：暗铁甲与赭红布，近战兵短剑圆盾、弓兵长弓箭袋、精英重甲长戟；每类各有东南向待机和攻击关键姿态，透明背景。
- **环境**：长坂坡残村、土黄灰绿和烟灰色、左上柔光、斜俯视；残屋、破车、道路地面、阿斗襁褓布包及无文字撤离路标。布包不展示可辨婴儿面部。

## 文件与接入

| 类型 | 源图目录 | Unity 内用途 |
| --- | --- | --- |
| 赵云 11 张 RGBA 源姿态 | `Assets/Game/Art/Characters/ZhaoYun/AnimationSources` | `AnimationS/ZhaoYunSEAtlas.asset`、7 个 `.anim` 和 Animator Controller；原图保留。 |
| 敌兵 6 张 RGBA 姿态 | `Assets/Game/Art/Enemies/Generated` | 三类敌兵预制体待机与预警/攻击切换。 |
| 环境与目标 5 张 RGBA/不透明图片 | `Assets/Game/Art/Environment/Generated` | 主场景残屋、路障、土路、救援点、出口。 |
| 赵云北向步行 3 帧 | `Assets/Game/Art/Characters/ZhaoYun/AnimationSources/WalkNorthV3` | 北向双脚交替接地、过步循环，独立 `WalkNorth.anim`。 |
| 赵云左右跑动 6 帧 V4 | `Assets/Game/Art/Characters/ZhaoYun/AnimationSources/RunEastV4` | 新向右六帧循环、向左镜像；0.48 秒一轮，统一缩放比例，逐帧对齐脚底轴点。原图与生成要求保存在 SOURCE.md。 |
| 赵云南向步行 4 帧 | `Assets/Game/Art/Characters/ZhaoYun/AnimationSources/WalkSouthV3` | 南向双脚交替接地、过步循环，独立 `WalkSouth.anim`。 |
| 赵云第 2、3 段攻击各向关键帧 | `Assets/Game/Art/Characters/ZhaoYun/AnimationSources/ComboV3`、`ComboNorthV3`、`ComboSouthV3` | 东/西、北、南三个朝向的上挑与低身平刺；通过独立图集和 Animator 状态接入。 |

图集和场景/预制体均由 Unity Editor API 生成，`.meta` 由 Unity 自动产生。地面源图全不透明且并非无缝贴图；残屋、破车和目标道具有弱半透明暖色晕边。正式美术验收仍须在实际游戏尺寸、三种底色及连续播放中检查。

新增赵云动作图由内置 ImageGen 以身份母版和已有方向动作图为参考生成，逐帧来源、透明边缘和脚底 pivot 见各目录的 `SOURCE.md`。北、南向步行使用独立帧，攻击第 2 段为扫/挑枪、第 3 段为蓄力突刺；斜向仍取最接近的四向素材。

V4 待机使用同一张原图的新 Sprite 资产，轴点从右靴附近调整到双脚之间，原图及旧图集不改。新跑动 sheet 按行留白测量切片，第 3、4 帧用 Sprite 网格排除下一行羽毛串入，同时保留第 4 帧两只靴子；不覆盖源 PNG。跑动不叠加旧姿态残影。第 3 段 J 的独立银灰气流、银白闪屑和脚尘复用既有 Kenney 资源，约 0.26–0.38 秒淡出。

## 可见边界与粒子（2026-09-29）

- `Assets/Game/Art/Environment/Generated/BattlefieldPalisadeHorizontal.png` 和 `BattlefieldPalisadeVertical.png` 使用内置 imagegen 生成，未覆盖旧图。提示词分别要求：三国长坂坡偏写实武侠风、透明背景、可重复拼接的横向残木栅栏和土石矮墙；以及相同材质、地面走向从画面底部通向顶部、木桩保持直立的纵向栅栏。避开人物、文字、旗帜、天空与发光效果。它们仅用作场景边缘，不替代角色设定母版。
- 主角普通攻击、L「破阵突刺」和敌兵出手/命中使用 Unity `ParticleSystem`。仅选用 [Kenney Particle Pack](https://kenney.nl/assets/particle-pack) 的 `star_01.png`、`dirt_01.png`、`smoke_03.png`、`circle_02.png`，位于 `Assets/Game/Art/VFX/Kenney/Resources/PlayerParticles`；资源为 CC0。包内 `License.txt` 与 `SOURCE.md` 记录许可和原始地址。未把整包导入工程。
- K 闪避新增脚下扬尘和灰白气流，玩家受击新增暖红火花、冲击环、角色闪色及屏幕边缘短闪。敌兵方向预警、出手、命中和箭矢尾迹均由粒子发射；旧的方向光线、攻击光条、箭矢贴图拖尾与发光预警圆盘停用。

最终使用的两张边界图由内置 imagegen 生成。以下保留实际提示词；纵向图以横向图为参考图。

**横向栅栏**

```text
Use case: stylized-concept
Asset type: repeatable horizontal 2D game environment sprite with genuine transparent background
Primary request: a single short section of ruined battlefield perimeter fence for a Three Kingdoms Changban slope action game. Weathered dark timber stakes and a shallow packed-earth and stone berm, visibly impassable but low enough not to conceal the hero. Broken uneven tips and a few rope lashings, cohesive shape. Viewed from a moderately elevated 3/4 perspective suitable for a 2D pseudo-3D Unity game; front face and top surfaces visible. Side edges visually repeatable when tiled. Restrained realistic wuxia concept-art painting, clean hand-painted material detail, muted ochre brown, slate grey and dried-grass colors, softly lit and grounded. Wide horizontal framing, object centered, closely fills canvas with transparent padding only around silhouette. No people, characters, weapons, flags, sky, scenery, ground plane, cast shadow outside sprite, text, icons or watermark. No bright magical glow. Transparent alpha background, isolated single asset.
```

**纵向栅栏**

```text
Use case: stylized-concept
Asset type: portrait-oriented transparent 2D pseudo-3D game sprite for a west/east map boundary
Create a short section of the same upright ruined timber fence from the reference, but its GROUND PATH must run vertically in the image: from the center of the BOTTOM image edge straight toward the center of the TOP image edge, like a north-south road viewed from an elevated game camera. The wooden stakes remain vertically upright in world space. Show about eight posts diminishing slightly into depth, a narrow earth-and-stone berm, ropes and dry weeds. Portrait canvas, object in a narrow central column, top and bottom endpoints suited to repeated placement. The output should be obviously taller than wide. Match muted realistic Three Kingdoms war-zone hand-painted material, no sideward diagonal composition. Isolated on truly transparent alpha. No people, sky, broad background, ground plane outside the fence, text, watermark, magical glow.
```
