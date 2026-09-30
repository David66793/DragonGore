# Zhao Yun south-facing combo 2–3 keyframes

Four transparent RGBA PNGs were generated for this project with Codex built-in ImageGen on 2026-09-29. They are original project-bound animation sources, not third-party assets. The character identity reference was `ArtSource/References/ZhaoYun_Character_Reference.png`; the pose and camera reference was `AnimationSources/AttackV2/ZY_S_AttackV2_Recovery.png` or `ZY_S_AttackV2_Extend.png`. The reference artwork was never overwritten.

| File | Motion beat | Full canvas |
| --- | --- | --- |
| `ZY_S_Combo2_Windup.png` | Pull spear across waist, wind hips for low sweep | 1536 × 1024 |
| `ZY_S_Combo2_Sweep.png` | Sweep toward lower-right enemy chest | 1536 × 1024 |
| `ZY_S_Combo3_Windup.png` | Recover to right shoulder, load forward leg | 1536 × 1024 |
| `ZY_S_Combo3_Lunge.png` | Drive through full-body forward thrust toward lower-center enemy chest | 1536 × 1024 |

Generation prompt set: one complete south-facing adult Zhao Yun game sprite on a 1536 × 1024 landscape transparent canvas; realistic painted wuxia style; preserve the reference's silver lamellar armor, off-white robe, muted blue-green cape and scarf, silver helmet and white plume, dark boots, continuous dark wood spear and silver tip; fixed elevated three-quarter camera, visible face and chest, grounded knees and weight shift, both hands gripping one spear. Pose-specific beats are in the table. The final lunge was regenerated once to keep the spear point inside the lower border. Avoid background, ground shadow, glow, light column, enemies, duplicate limbs or weapon, and text.

Technical QA: Pillow verified all four as 1536 × 1024 RGBA with transparent corner pixels and both zero and nonzero alpha. At alpha ≥128, the content bounding boxes are approximately (108, 19)–(1377, 1004), (71, 14)–(1501, 1005), (93, 24)–(1385, 1009), and (90, 18)–(1317, 992) respectively. The final lunge has no opaque pixel on its bottom row. These checks cover file and canvas integrity, not in-engine visual acceptance.

Recommended Unity sprite pivot for all four: normalized `(0.52, 0.014)` (pixel `(799, 14)` measured from bottom-left), matching the existing south attack atlas. The painted boots' lower silhouette varies as each stance shifts; inspect transitions at game scale before treating this as final animation polish. Use the same character world height/PPU as the current south attack frames, and keep shadow separate from artwork.
