# Zhao Yun north facing combo 2 and 3 source

Generated on 2026-09-29 with the built-in OpenAI image generation tool. These are new animation key poses for the project's 2D pseudo-isometric Zhao Yun, continuing the existing north facing attack art. The source identity references were `ArtSource/References/ZhaoYun_Character_Reference.png` and `AnimationSources/AttackV2/ZY_N_AttackV2_Windup.png` / `ZY_N_AttackV2_Extend.png`. The generated poses were also used as sequential references to retain costume and visual continuity. No archived art was imported into Unity.

| File | Pose prompt | Intended transition |
| --- | --- | --- |
| `ZY_N_Combo2_Windup.png` | Back view, lowered weight, rotate torso and draw the spear across the waist to prepare a rising cross-body sweep. | Hit 1 recovery → combo 2 preparation |
| `ZY_N_Combo2_Sweep.png` | Back view, transfer weight forward and finish a spear sweep toward the upper right at enemy chest height. | Combo 2 active frame |
| `ZY_N_Combo3_Windup.png` | Back view, plant rear foot and coil the spear along the right shoulder for a straight thrust. | Combo 2 sweep → combo 3 preparation |
| `ZY_N_Combo3_Lunge.png` | Back view, deep forward step and extend both arms with the spear diagonally toward the upper right at enemy chest height. | Combo 3 active frame |

All four PNGs are 1536 × 1024 RGBA with real alpha; corner alpha is 0 and alpha above 32 does not reach a canvas edge. Body scale and foot placement were checked against the north facing `AttackV2` frames. Use the same sprite pivot as those frames: normalized **(0.52, 0.014)**, equivalent to canvas position about **(799, 1010)** measured from the upper left. This keeps the world foot anchor stable while the two boots separate during combat.

These are key poses rather than a full hand-drawn sequence. The animation controller should supply timing and reuse recovery frames as needed. A visual check at actual in-game scale is still required after atlas import, especially for spear visibility and frame-to-frame foot registration. The PNGs do not contain attack effects; those belong to runtime particles.
