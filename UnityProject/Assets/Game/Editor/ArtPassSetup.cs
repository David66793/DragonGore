using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Changban.EditorTools
{
    // Applies generated art to the existing demo without recreating its scenes or prefabs.
    public static class ArtPassSetup
    {
        private const string Root = "Assets/Game";
        private const string Source = Root + "/Art/Characters/ZhaoYun/AnimationSources";
        private const string Anim = Root + "/Art/Characters/ZhaoYun/Animations";
        private const string Enemy = Root + "/Art/Enemies/Generated";
        private const string Environment = Root + "/Art/Environment/Generated";
        private const string MainScene = Root + "/Scenes/ChangbanDemo.unity";
        private const string ArtScene = Root + "/Scenes/Tests/ArtValidation.unity";
        private const string AtlasPath = Anim + "/ZhaoYunSEAtlas.asset";
        private const string AttackAtlasPath = Anim + "/ZhaoYunAttackV2Atlas.asset";
        private const string VerticalAttackAtlasPath = Anim + "/ZhaoYunVerticalAttackV2Atlas.asset";
        private const string NorthWalkAtlasPath = Anim + "/ZhaoYunNorthWalkV3Atlas.asset";
        private const string SouthWalkAtlasPath = Anim + "/ZhaoYunSouthWalkV3Atlas.asset";
        private const string ComboAtlasPath = Anim + "/ZhaoYunComboV3Atlas.asset";
        private const string NorthComboAtlasPath = Anim + "/ZhaoYunNorthComboV3Atlas.asset";
        private const string SouthComboAtlasPath = Anim + "/ZhaoYunSouthComboV3Atlas.asset";
        private const string ControllerPath = Anim + "/ZhaoYunSE.controller";
        private static Dictionary<string, Sprite> poses;
        private static Dictionary<string, Sprite> attackPoses;
        private static Dictionary<string, Sprite> verticalAttackPoses;
        private static Dictionary<string, Sprite> northWalkPoses;
        private static Dictionary<string, Sprite> southWalkPoses;
        private static Dictionary<string, Sprite> comboPoses;
        private static Dictionary<string, Sprite> northComboPoses;
        private static Dictionary<string, Sprite> southComboPoses;
        private static Material characterMaterial;
        private static Material attackMaterial;
        private static Material verticalAttackMaterial;
        private static Material northWalkMaterial;
        private static Material southWalkMaterial;
        private static Material comboMaterial;
        private static Material northComboMaterial;
        private static Material southComboMaterial;

        [MenuItem("Changban/Apply Art Pass")]
        public static void Apply()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(MainScene) == null)
                throw new InvalidOperationException("Run Changban/Setup Demo first.");
            EnsureFolder(Anim);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            poses = EnsureAtlas();
            characterMaterial = EnsureMaterial(Anim + "/ZhaoYunSEUnlit.mat",
                AssetDatabase.LoadAssetAtPath<Texture2D>(AtlasPath));
            attackPoses = EnsureAttackAtlas();
            attackMaterial = EnsureMaterial(Anim + "/ZhaoYunAttackV2Unlit.mat",
                AssetDatabase.LoadAssetAtPath<Texture2D>(AttackAtlasPath));
            verticalAttackPoses = EnsureVerticalAttackAtlas();
            verticalAttackMaterial = EnsureMaterial(Anim + "/ZhaoYunVerticalAttackV2Unlit.mat",
                AssetDatabase.LoadAssetAtPath<Texture2D>(VerticalAttackAtlasPath));
            northWalkPoses = EnsureGeneratedAtlas(NorthWalkAtlasPath, "WalkNorthV3",
                new[] { "ZY_N_WalkV3_ContactA", "ZY_N_WalkV3_Passing", "ZY_N_WalkV3_ContactB" },
                new[] { new Vector2(0.56f, 0.022f), new Vector2(0.56f, 0.044f), new Vector2(0.56f, 0.030f) });
            southWalkPoses = EnsureGeneratedAtlas(SouthWalkAtlasPath, "WalkSouthV3",
                new[] { "ZY_S_WalkV3_ContactLeft", "ZY_S_WalkV3_Pass", "ZY_S_WalkV3_ContactRight", "ZY_S_WalkV3_PassOther" },
                new[] { new Vector2(0.50f, 0.03f), new Vector2(0.50f, 0.03f),
                    new Vector2(0.50f, 0.03f), new Vector2(0.50f, 0.03f) });
            comboPoses = EnsureGeneratedAtlas(ComboAtlasPath, "ComboV3",
                new[] { "ZY_SE_Combo2_Windup", "ZY_SE_Combo2_Sweep", "ZY_SE_Combo3_Windup", "ZY_SE_Combo3_Lunge",
                    "ZY_SE_AttackV2_Recovery" },
                new[] { new Vector2(0.50f, 0.039f), new Vector2(0.50f, 0.043f),
                    new Vector2(0.50f, 0.078f), new Vector2(0.50f, 0.142f), new Vector2(0.455f, 0.038f) });
            northComboPoses = EnsureGeneratedAtlas(NorthComboAtlasPath, "ComboNorthV3",
                new[] { "ZY_N_Combo2_Windup", "ZY_N_Combo2_Sweep", "ZY_N_Combo3_Windup", "ZY_N_Combo3_Lunge",
                    "ZY_N_AttackV2_Recovery" },
                new[] { new Vector2(0.52f, 0.014f), new Vector2(0.52f, 0.014f),
                    new Vector2(0.52f, 0.014f), new Vector2(0.52f, 0.014f), new Vector2(0.52f, 0.014f) });
            southComboPoses = EnsureGeneratedAtlas(SouthComboAtlasPath, "ComboSouthV3",
                new[] { "ZY_S_Combo2_Windup", "ZY_S_Combo2_Sweep", "ZY_S_Combo3_Windup", "ZY_S_Combo3_Lunge",
                    "ZY_S_AttackV2_Recovery" },
                new[] { new Vector2(0.52f, 0.014f), new Vector2(0.52f, 0.014f),
                    new Vector2(0.52f, 0.014f), new Vector2(0.52f, 0.014f), new Vector2(0.52f, 0.014f) });
            northWalkMaterial = EnsureMaterial(Anim + "/ZhaoYunNorthWalkV3Unlit.mat",
                AssetDatabase.LoadAssetAtPath<Texture2D>(NorthWalkAtlasPath));
            southWalkMaterial = EnsureMaterial(Anim + "/ZhaoYunSouthWalkV3Unlit.mat",
                AssetDatabase.LoadAssetAtPath<Texture2D>(SouthWalkAtlasPath));
            comboMaterial = EnsureMaterial(Anim + "/ZhaoYunComboV3Unlit.mat",
                AssetDatabase.LoadAssetAtPath<Texture2D>(ComboAtlasPath));
            northComboMaterial = EnsureMaterial(Anim + "/ZhaoYunNorthComboV3Unlit.mat",
                AssetDatabase.LoadAssetAtPath<Texture2D>(NorthComboAtlasPath));
            southComboMaterial = EnsureMaterial(Anim + "/ZhaoYunSouthComboV3Unlit.mat",
                AssetDatabase.LoadAssetAtPath<Texture2D>(SouthComboAtlasPath));
            AnimatorController controller = EnsureController();

            Sprite melee = ImportSprite(Enemy + "/Enemy_Melee_SE_v01.png", new Vector2(0.50f, 0.08f), 700);
            Sprite archer = ImportSprite(Enemy + "/Enemy_Archer_SE_v01.png", new Vector2(0.50f, 0.08f), 700);
            Sprite elite = ImportSprite(Enemy + "/Enemy_Elite_SE_v01.png", new Vector2(0.50f, 0.08f), 700);
            Sprite meleeAttack = ImportOptional(Enemy + "/Enemy_Melee_Attack_SE.png", new Vector2(0.50f, 0.075f), 700);
            Sprite archerAttack = ImportOptional(Enemy + "/Enemy_Archer_Attack_SE.png", new Vector2(0.50f, 0.066f), 700);
            Sprite eliteAttack = ImportOptional(Enemy + "/Enemy_Elite_Attack_SE.png", new Vector2(0.50f, 0.034f), 700);
            Sprite meleeStrike = ImportSprite(Enemy + "/AttackV2/Enemy_Melee_Strike_SE_v02.png", new Vector2(0.50f, 0.09f), 700);
            Sprite meleeRecover = ImportSprite(Enemy + "/AttackV2/Enemy_Melee_Recover_SE_v02.png", new Vector2(0.50f, 0.09f), 700);
            Sprite archerRelease = ImportSprite(Enemy + "/AttackV2/Enemy_Archer_Release_SE_v02.png", new Vector2(0.50f, 0.09f), 700);
            Sprite archerRecover = ImportSprite(Enemy + "/AttackV2/Enemy_Archer_Recover_SE_v02.png", new Vector2(0.50f, 0.09f), 700);
            Sprite eliteStrike = ImportSprite(Enemy + "/AttackV2/Enemy_Elite_Strike_SE_v02.png", new Vector2(0.50f, 0.09f), 700);
            Sprite eliteRecover = ImportSprite(Enemy + "/AttackV2/Enemy_Elite_Recover_SE_v02.png", new Vector2(0.50f, 0.09f), 700);
            Sprite house = ImportSprite(Environment + "/RuinedVillageHouse.png", new Vector2(0.50f, 0.12f), 350);
            Sprite cart = ImportSprite(Environment + "/BrokenCartBarricade.png", new Vector2(0.50f, 0.12f), 350);
            Sprite ground = ImportSprite(Environment + "/DirtRoadGroundPatch.png", new Vector2(0.50f, 0.50f), 500);
            Sprite continuousGround = ImportSprite(Environment + "/SeamlessDirtGround.png", new Vector2(0.50f, 0.50f), 500);
            Sprite rescue = ImportSprite(Environment + "/A_DouSwaddleBasket.png", new Vector2(0.50f, 0.15f), 900);
            Sprite exit = ImportSprite(Environment + "/EvacuationWaymarker.png", new Vector2(0.50f, 0.08f), 800);
            Sprite fenceHorizontal = ImportSprite(Environment + "/BattlefieldPalisadeHorizontal.png",
                new Vector2(0.50f, 0.08f), 500);
            Sprite fenceVertical = ImportSprite(Environment + "/BattlefieldPalisadeVertical.png",
                new Vector2(0.50f, 0.50f), 500);

            ApplyPlayerPrefab(controller);
            ApplyEnemyPrefab("Melee", melee, meleeAttack, meleeStrike, meleeRecover);
            ApplyEnemyPrefab("Archer", archer, archerAttack, archerRelease, archerRecover);
            ApplyEnemyPrefab("Elite", elite, eliteAttack, eliteStrike, eliteRecover);
            ApplyMainScene(house, cart, ground, continuousGround, rescue, exit,
                fenceHorizontal, fenceVertical);
            ApplyArtScene();
            AnimationPolishSetup.ApplyToCurrentArt();
            GameUiSetup.Apply();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorSceneManager.OpenScene(MainScene);
            Debug.Log("Changban art pass complete: Animator clips, character and enemy art, environment and goal props applied.");
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = path.Substring(0, path.LastIndexOf('/'));
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, path.Substring(path.LastIndexOf('/') + 1));
        }

        private static Dictionary<string, Sprite> EnsureAtlas()
        {
            Texture2D existing = AssetDatabase.LoadAssetAtPath<Texture2D>(AtlasPath);
            if (existing == null)
            {
                string sourceFolder = Path.Combine(Application.dataPath, Source.Substring("Assets/".Length));
                string[] files = Directory.GetFiles(sourceFolder, "ZY_SE_*.png").OrderBy(p => p).ToArray();
                if (files.Length < 11) throw new InvalidOperationException("Expected 11 Zhao Yun source poses, found " + files.Length);
                var sourceTextures = new List<Texture2D>();
                try
                {
                    foreach (string path in files)
                    {
                        var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                        if (!ImageConversion.LoadImage(texture, File.ReadAllBytes(path), false))
                            throw new InvalidOperationException("Cannot decode " + path);
                        sourceTextures.Add(texture);
                    }
                    var atlas = new Texture2D(4096, 4096, TextureFormat.RGBA32, false)
                    { name = "Zhao Yun SE animation atlas", filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
                    Rect[] regions = atlas.PackTextures(sourceTextures.ToArray(), 4, 4096, false);
                    atlas.Apply(false, true);
                    AssetDatabase.CreateAsset(atlas, AtlasPath);
                    for (int i = 0; i < files.Length; i++)
                    {
                        string name = Path.GetFileNameWithoutExtension(files[i]);
                        Rect region = regions[i];
                        Rect pixels = new Rect(Mathf.Round(region.x * atlas.width), Mathf.Round(region.y * atlas.height),
                            Mathf.Round(region.width * atlas.width), Mathf.Round(region.height * atlas.height));
                        Sprite sprite = Sprite.Create(atlas, pixels, PivotFor(name), 520f, 0, SpriteMeshType.FullRect);
                        sprite.name = name;
                        AssetDatabase.AddObjectToAsset(sprite, atlas);
                    }
                    EditorUtility.SetDirty(atlas);
                    AssetDatabase.SaveAssets();
                    Debug.Log("Packed " + files.Length + " Zhao Yun source poses into one 4096 px animation atlas.");
                }
                finally
                {
                    foreach (Texture2D texture in sourceTextures) UnityEngine.Object.DestroyImmediate(texture);
                }
            }
            return AssetDatabase.LoadAllAssetsAtPath(AtlasPath).OfType<Sprite>().ToDictionary(s => s.name, s => s);
        }

        private static Dictionary<string, Sprite> EnsureAttackAtlas()
        {
            Texture2D existing = AssetDatabase.LoadAssetAtPath<Texture2D>(AttackAtlasPath);
            if (existing == null)
            {
                string folder = Path.Combine(Application.dataPath, Source.Substring("Assets/".Length), "AttackV2");
                string[] labels = { "Coil", "Windup", "Extend", "Recovery" };
                var sources = new List<Texture2D>();
                try
                {
                    foreach (string label in labels)
                    {
                        string path = Path.Combine(folder, "ZY_SE_AttackV2_" + label + ".png");
                        if (!File.Exists(path)) throw new FileNotFoundException("Missing attack pose", path);
                        var image = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                        if (!ImageConversion.LoadImage(image, File.ReadAllBytes(path), false))
                            throw new InvalidOperationException("Cannot decode " + path);
                        sources.Add(image);
                    }
                    var atlas = new Texture2D(4096, 4096, TextureFormat.RGBA32, false)
                    { name = "Zhao Yun chest-height attack atlas", filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
                    Rect[] regions = atlas.PackTextures(sources.ToArray(), 4, 4096, false);
                    atlas.Apply(false, true);
                    AssetDatabase.CreateAsset(atlas, AttackAtlasPath);
                    float idleHeight = Pose("Idle_A").bounds.size.y;
                    for (int i = 0; i < labels.Length; i++)
                    {
                        Rect region = regions[i];
                        Rect pixels = new Rect(Mathf.Round(region.x * atlas.width), Mathf.Round(region.y * atlas.height),
                            Mathf.Round(region.width * atlas.width), Mathf.Round(region.height * atlas.height));
                        float ppu = pixels.height / idleHeight;
                        Vector2 pivot = new Vector2(0.455f, labels[i] == "Recovery" ? 0.038f : 0.028f);
                        Sprite sprite = Sprite.Create(atlas, pixels, pivot, ppu, 0, SpriteMeshType.FullRect);
                        sprite.name = "ZY_SE_AttackV2_" + labels[i];
                        AssetDatabase.AddObjectToAsset(sprite, atlas);
                    }
                    EditorUtility.SetDirty(atlas);
                    AssetDatabase.SaveAssets();
                    Debug.Log("Packed four chest-height Zhao Yun attack poses.");
                }
                finally { foreach (Texture2D image in sources) UnityEngine.Object.DestroyImmediate(image); }
            }
            return AssetDatabase.LoadAllAssetsAtPath(AttackAtlasPath).OfType<Sprite>().ToDictionary(s => s.name, s => s);
        }

        private static Sprite AttackPose(string label)
        {
            string key = "ZY_SE_AttackV2_" + label;
            if (!attackPoses.TryGetValue(key, out Sprite sprite)) throw new InvalidOperationException("Attack pose missing: " + key);
            return sprite;
        }

        private static Dictionary<string, Sprite> EnsureVerticalAttackAtlas()
        {
            Texture2D existing = AssetDatabase.LoadAssetAtPath<Texture2D>(VerticalAttackAtlasPath);
            if (existing == null)
            {
                string folder = Path.Combine(Application.dataPath, Source.Substring("Assets/".Length), "AttackV2");
                string[] names = { "ZY_N_AttackV2_Windup", "ZY_N_AttackV2_Extend", "ZY_N_AttackV2_Recovery",
                    "ZY_S_AttackV2_Windup", "ZY_S_AttackV2_Extend", "ZY_S_AttackV2_Recovery" };
                var sources = new List<Texture2D>();
                try
                {
                    foreach (string name in names)
                    {
                        string path = Path.Combine(folder, name + ".png");
                        if (!File.Exists(path)) throw new FileNotFoundException("Missing vertical attack pose", path);
                        var image = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                        if (!ImageConversion.LoadImage(image, File.ReadAllBytes(path), false))
                            throw new InvalidOperationException("Cannot decode " + path);
                        sources.Add(image);
                    }
                    var atlas = new Texture2D(4096, 4096, TextureFormat.RGBA32, false)
                    { name = "Zhao Yun north and south attack atlas", filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
                    Rect[] regions = atlas.PackTextures(sources.ToArray(), 4, 4096, false);
                    atlas.Apply(false, true);
                    AssetDatabase.CreateAsset(atlas, VerticalAttackAtlasPath);
                    float idleHeight = Pose("Idle_A").bounds.size.y;
                    for (int i = 0; i < names.Length; i++)
                    {
                        Rect region = regions[i];
                        Rect pixels = new Rect(Mathf.Round(region.x * atlas.width), Mathf.Round(region.y * atlas.height),
                            Mathf.Round(region.width * atlas.width), Mathf.Round(region.height * atlas.height));
                        Sprite sprite = Sprite.Create(atlas, pixels, new Vector2(0.52f, 0.014f),
                            pixels.height / idleHeight, 0, SpriteMeshType.FullRect);
                        sprite.name = names[i];
                        AssetDatabase.AddObjectToAsset(sprite, atlas);
                    }
                    EditorUtility.SetDirty(atlas);
                    AssetDatabase.SaveAssets();
                    Debug.Log("Packed six north/south Zhao Yun attack poses.");
                }
                finally { foreach (Texture2D image in sources) UnityEngine.Object.DestroyImmediate(image); }
            }
            return AssetDatabase.LoadAllAssetsAtPath(VerticalAttackAtlasPath).OfType<Sprite>().ToDictionary(s => s.name, s => s);
        }

        private static Sprite VerticalAttackPose(string direction, string label)
        {
            string key = "ZY_" + direction + "_AttackV2_" + label;
            if (!verticalAttackPoses.TryGetValue(key, out Sprite sprite))
                throw new InvalidOperationException("Vertical attack pose missing: " + key);
            return sprite;
        }

        private static Dictionary<string, Sprite> EnsureGeneratedAtlas(string atlasPath, string sourceFolder,
            string[] names, Vector2[] pivots)
        {
            if (names.Length != pivots.Length) throw new ArgumentException("Every generated pose needs a pivot");
            Texture2D existing = AssetDatabase.LoadAssetAtPath<Texture2D>(atlasPath);
            if (existing == null)
            {
                string folder = Path.Combine(Application.dataPath, Source.Substring("Assets/".Length), sourceFolder);
                var sources = new List<Texture2D>();
                try
                {
                    foreach (string name in names)
                    {
                        string file = Path.Combine(folder, name + ".png");
                        if (name.EndsWith("AttackV2_Recovery"))
                            file = Path.Combine(Application.dataPath, Source.Substring("Assets/".Length),
                                "AttackV2", name + ".png");
                        if (!File.Exists(file)) throw new FileNotFoundException("Generated pose is missing", file);
                        var image = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                        if (!ImageConversion.LoadImage(image, File.ReadAllBytes(file), false))
                            throw new InvalidOperationException("Cannot decode generated pose " + file);
                        sources.Add(image);
                    }
                    var atlas = new Texture2D(4096, 4096, TextureFormat.RGBA32, false)
                    { name = sourceFolder + " animation atlas", filterMode = FilterMode.Bilinear,
                        wrapMode = TextureWrapMode.Clamp };
                    Rect[] regions = atlas.PackTextures(sources.ToArray(), 4, 4096, false);
                    atlas.Apply(false, true);
                    AssetDatabase.CreateAsset(atlas, atlasPath);
                    float idleHeight = Pose("Idle_A").bounds.size.y;
                    for (int i = 0; i < names.Length; i++)
                    {
                        Rect r = regions[i];
                        Rect pixels = new Rect(Mathf.Round(r.x * atlas.width), Mathf.Round(r.y * atlas.height),
                            Mathf.Round(r.width * atlas.width), Mathf.Round(r.height * atlas.height));
                        Sprite sprite = Sprite.Create(atlas, pixels, pivots[i], pixels.height / idleHeight,
                            0, SpriteMeshType.FullRect);
                        sprite.name = names[i];
                        AssetDatabase.AddObjectToAsset(sprite, atlas);
                    }
                    EditorUtility.SetDirty(atlas);
                    AssetDatabase.SaveAssets();
                    Debug.Log("Packed " + names.Length + " " + sourceFolder + " Zhao Yun poses.");
                }
                finally { foreach (Texture2D image in sources) UnityEngine.Object.DestroyImmediate(image); }
            }
            var result = AssetDatabase.LoadAllAssetsAtPath(atlasPath).OfType<Sprite>()
                .ToDictionary(sprite => sprite.name, sprite => sprite);
            foreach (string name in names)
                if (!result.ContainsKey(name)) throw new InvalidOperationException("Atlas pose missing: " + name);
            return result;
        }

        private static Sprite GeneratedPose(Dictionary<string, Sprite> atlas, string name)
        {
            if (!atlas.TryGetValue(name, out Sprite sprite)) throw new InvalidOperationException("Pose missing: " + name);
            return sprite;
        }

        private static Vector2 PivotFor(string name)
        {
            switch (name)
            {
                case "ZY_SE_Walk_Contact_A": return new Vector2(0.64f, 0.03f);
                case "ZY_SE_Walk_Contact_B": return new Vector2(0.69f, 0.02f);
                case "ZY_SE_Walk_Pass": return new Vector2(0.50f, 0.06f);
                case "ZY_SE_Thrust_Windup": return new Vector2(0.73f, 0.04f);
                case "ZY_SE_Thrust_Extend": return new Vector2(0.72f, 0.07f);
                case "ZY_SE_Thrust_Recovery": return new Vector2(0.74f, 0.06f);
                case "ZY_SE_Hurt": return new Vector2(0.70f, 0.04f);
                case "ZY_SE_Dodge": return new Vector2(0.16f, 0.11f);
                case "ZY_SE_Skill_Lunge": return new Vector2(0.70f, 0.13f);
                case "ZY_SE_Death": return new Vector2(0.50f, 0.13f);
                default: return new Vector2(0.66f, 0.04f);
            }
        }

        private static Sprite Pose(string suffix)
        {
            string name = "ZY_SE_" + suffix;
            if (!poses.TryGetValue(name, out Sprite sprite)) throw new InvalidOperationException("Pose missing from atlas: " + name);
            return sprite;
        }

        private static AnimatorController EnsureController()
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (controller != null)
            {
                UpdateAttackClip();
                UpdateSkillClip();
                EnsureVerticalAttackStates(controller);
                EnsureV3States(controller);
                return controller;
            }
            var clips = new Dictionary<string, AnimationClip>
            {
                ["Idle"] = CreateClip("Idle", true, new[] { Pose("Idle_A"), Pose("Idle_A") }, new[] { 0f, 0.8f }),
                ["Walk"] = CreateClip("Walk", true,
                    new[] { Pose("Walk_Contact_A"), Pose("Walk_Pass"), Pose("Walk_Contact_B"), Pose("Walk_Pass"), Pose("Walk_Contact_A") },
                    new[] { 0f, 0.15f, 0.30f, 0.45f, 0.60f }),
                ["Attack"] = CreateClip("Attack", false,
                    new[] { AttackPose("Coil"), AttackPose("Windup"), AttackPose("Extend"), AttackPose("Recovery") },
                    new[] { 0f, 0.08f, 0.17f, 0.29f }),
                ["Dodge"] = CreateClip("Dodge", false,
                    new[] { Pose("Dodge"), Pose("Dodge"), Pose("Idle_A") }, new[] { 0f, 0.15f, 0.24f }),
                ["Skill"] = CreateClip("Skill", false,
                    new[] { AttackPose("Coil"), AttackPose("Windup"), AttackPose("Extend"),
                        AttackPose("Extend"), AttackPose("Recovery") },
                    new[] { 0f, 0.06f, 0.12f, 0.23f, 0.34f }),
                ["Hurt"] = CreateClip("Hurt", false,
                    new[] { Pose("Hurt"), Pose("Hurt"), Pose("Idle_A") }, new[] { 0f, 0.16f, 0.25f }),
                ["Death"] = CreateClip("Death", false,
                    new[] { Pose("Hurt"), Pose("Death"), Pose("Death") }, new[] { 0f, 0.16f, 0.6f })
            };
            controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            AnimatorStateMachine machine = controller.layers[0].stateMachine;
            foreach (var pair in clips)
            {
                AnimatorState state = machine.AddState(pair.Key);
                state.motion = pair.Value;
                if (pair.Key == "Idle") machine.defaultState = state;
            }
            EnsureVerticalAttackStates(controller);
            EnsureV3States(controller);
            EditorUtility.SetDirty(controller);
            return controller;
        }

        private static void EnsureV3States(AnimatorController controller)
        {
            EnsureClipState(controller, "WalkNorth", true,
                new[] { GeneratedPose(northWalkPoses, "ZY_N_WalkV3_ContactA"),
                    GeneratedPose(northWalkPoses, "ZY_N_WalkV3_Passing"),
                    GeneratedPose(northWalkPoses, "ZY_N_WalkV3_ContactB"),
                    GeneratedPose(northWalkPoses, "ZY_N_WalkV3_Passing"),
                    GeneratedPose(northWalkPoses, "ZY_N_WalkV3_ContactA") },
                new[] { 0f, 0.12f, 0.24f, 0.36f, 0.48f });
            EnsureClipState(controller, "WalkSouth", true,
                new[] { GeneratedPose(southWalkPoses, "ZY_S_WalkV3_ContactLeft"),
                    GeneratedPose(southWalkPoses, "ZY_S_WalkV3_Pass"),
                    GeneratedPose(southWalkPoses, "ZY_S_WalkV3_ContactRight"),
                    GeneratedPose(southWalkPoses, "ZY_S_WalkV3_PassOther"),
                    GeneratedPose(southWalkPoses, "ZY_S_WalkV3_ContactLeft") },
                new[] { 0f, 0.12f, 0.24f, 0.36f, 0.48f });
            Sprite comboRecovery = GeneratedPose(comboPoses, "ZY_SE_AttackV2_Recovery");
            EnsureClipState(controller, "Attack2", false,
                new[] { GeneratedPose(comboPoses, "ZY_SE_Combo2_Windup"),
                    GeneratedPose(comboPoses, "ZY_SE_Combo2_Sweep"),
                    GeneratedPose(comboPoses, "ZY_SE_Combo2_Sweep"), comboRecovery },
                new[] { 0f, 0.17f, 0.30f, 0.43f });
            EnsureClipState(controller, "Attack3", false,
                new[] { GeneratedPose(comboPoses, "ZY_SE_Combo3_Windup"),
                    GeneratedPose(comboPoses, "ZY_SE_Combo3_Lunge"),
                    GeneratedPose(comboPoses, "ZY_SE_Combo3_Lunge"), comboRecovery },
                new[] { 0f, 0.17f, 0.30f, 0.43f });
            foreach (string direction in new[] { "N", "S" })
            {
                string suffix = direction == "N" ? "North" : "South";
                Dictionary<string, Sprite> atlas = direction == "N" ? northComboPoses : southComboPoses;
                Sprite recovery = GeneratedPose(atlas, "ZY_" + direction + "_AttackV2_Recovery");
                EnsureClipState(controller, "Attack2" + suffix, false,
                    new[] { GeneratedPose(atlas, "ZY_" + direction + "_Combo2_Windup"),
                        GeneratedPose(atlas, "ZY_" + direction + "_Combo2_Sweep"),
                        GeneratedPose(atlas, "ZY_" + direction + "_Combo2_Sweep"), recovery },
                    new[] { 0f, 0.17f, 0.30f, 0.43f });
                EnsureClipState(controller, "Attack3" + suffix, false,
                    new[] { GeneratedPose(atlas, "ZY_" + direction + "_Combo3_Windup"),
                        GeneratedPose(atlas, "ZY_" + direction + "_Combo3_Lunge"),
                        GeneratedPose(atlas, "ZY_" + direction + "_Combo3_Lunge"), recovery },
                    new[] { 0f, 0.17f, 0.30f, 0.43f });
            }
        }

        private static void EnsureClipState(AnimatorController controller, string stateName, bool loop,
            Sprite[] frames, float[] times)
        {
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(Anim + "/" + stateName + ".anim");
            if (clip == null) clip = CreateClip(stateName, loop, frames, times);
            else UpdateSpriteClip(clip, frames, times);
            AnimatorStateMachine machine = controller.layers[0].stateMachine;
            AnimatorState state = machine.states.Select(item => item.state)
                .FirstOrDefault(item => item.name == stateName);
            if (state == null) state = machine.AddState(stateName);
            if (state.motion != clip) { state.motion = clip; EditorUtility.SetDirty(controller); }
        }

        private static void EnsureVerticalAttackStates(AnimatorController controller)
        {
            AnimatorStateMachine machine = controller.layers[0].stateMachine;
            foreach (string direction in new[] { "N", "S" })
            {
                string stateName = direction == "N" ? "AttackNorth" : "AttackSouth";
                Sprite[] frames = { VerticalAttackPose(direction, "Windup"),
                    VerticalAttackPose(direction, "Extend"), VerticalAttackPose(direction, "Recovery") };
                float[] times = { 0f, 0.17f, 0.29f };
                AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(Anim + "/" + stateName + ".anim");
                if (clip == null) clip = CreateClip(stateName, false, frames, times);
                else UpdateSpriteClip(clip, frames, times);
                AnimatorState state = machine.states.Select(s => s.state).FirstOrDefault(s => s.name == stateName);
                if (state == null) state = machine.AddState(stateName);
                if (state.motion != clip) { state.motion = clip; EditorUtility.SetDirty(controller); }
                string skillName = direction == "N" ? "SkillNorth" : "SkillSouth";
                Sprite[] skillFrames = { VerticalAttackPose(direction, "Windup"),
                    VerticalAttackPose(direction, "Extend"), VerticalAttackPose(direction, "Extend"),
                    VerticalAttackPose(direction, "Recovery") };
                float[] skillTimes = { 0f, 0.10f, 0.23f, 0.34f };
                AnimationClip skillClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(Anim + "/" + skillName + ".anim");
                if (skillClip == null) skillClip = CreateClip(skillName, false, skillFrames, skillTimes);
                else UpdateSpriteClip(skillClip, skillFrames, skillTimes);
                AnimatorState skillState = machine.states.Select(s => s.state).FirstOrDefault(s => s.name == skillName);
                if (skillState == null) skillState = machine.AddState(skillName);
                if (skillState.motion != skillClip) { skillState.motion = skillClip; EditorUtility.SetDirty(controller); }
            }
        }

        private static void UpdateSkillClip()
        {
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(Anim + "/Skill.anim");
            if (clip == null) throw new InvalidOperationException("Skill animation is missing");
            Sprite[] frames = { AttackPose("Coil"), AttackPose("Windup"), AttackPose("Extend"),
                AttackPose("Extend"), AttackPose("Recovery") };
            float[] times = { 0f, 0.06f, 0.12f, 0.23f, 0.34f };
            UpdateSpriteClip(clip, frames, times);
        }

        private static void UpdateAttackClip()
        {
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(Anim + "/Attack.anim");
            if (clip == null) throw new InvalidOperationException("Attack animation is missing");
            Sprite[] frames = { AttackPose("Coil"), AttackPose("Windup"), AttackPose("Extend"), AttackPose("Recovery") };
            float[] times = { 0f, 0.08f, 0.17f, 0.29f };
            UpdateSpriteClip(clip, frames, times);
        }

        private static void UpdateSpriteClip(AnimationClip clip, Sprite[] frames, float[] times)
        {
            var binding = new EditorCurveBinding { path = "", type = typeof(SpriteRenderer), propertyName = "m_Sprite" };
            ObjectReferenceKeyframe[] current = AnimationUtility.GetObjectReferenceCurve(clip, binding);
            if (current != null && current.Length == frames.Length &&
                current.Select((key, i) => key.value == frames[i] && Mathf.Abs(key.time - times[i]) < 0.001f).All(ok => ok)) return;
            var keys = new ObjectReferenceKeyframe[frames.Length];
            for (int i = 0; i < frames.Length; i++) keys[i] = new ObjectReferenceKeyframe { time = times[i], value = frames[i] };
            AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);
            EditorUtility.SetDirty(clip);
        }

        private static AnimationClip CreateClip(string name, bool loop, Sprite[] frames, float[] times)
        {
            string path = Anim + "/" + name + ".anim";
            var clip = new AnimationClip { frameRate = 30f, name = name };
            var binding = new EditorCurveBinding { path = "", type = typeof(SpriteRenderer), propertyName = "m_Sprite" };
            var keys = new ObjectReferenceKeyframe[frames.Length];
            for (int i = 0; i < frames.Length; i++)
                keys[i] = new ObjectReferenceKeyframe { time = times[i], value = frames[i] };
            AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);
            var serialized = new SerializedObject(clip);
            serialized.FindProperty("m_AnimationClipSettings.m_LoopTime").boolValue = loop;
            serialized.ApplyModifiedProperties();
            AssetDatabase.CreateAsset(clip, path);
            return clip;
        }

        private static Sprite ImportOptional(string path, Vector2 pivot, float ppu)
        {
            return File.Exists(Path.Combine(Application.dataPath, path.Substring("Assets/".Length))) ? ImportSprite(path, pivot, ppu) : null;
        }

        private static Sprite ImportSprite(string path, Vector2 pivot, float ppu)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) throw new FileNotFoundException("Art PNG was not imported", path);
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            bool needs = importer.textureType != TextureImporterType.Sprite ||
                         importer.spritePixelsPerUnit != ppu ||
                         (settings.spritePivot - pivot).sqrMagnitude > 0.000001f ||
                         settings.spriteAlignment != (int)SpriteAlignment.Custom;
            if (needs)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.filterMode = FilterMode.Bilinear;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.maxTextureSize = 2048;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                settings.spriteMeshType = SpriteMeshType.FullRect;
                settings.spriteAlignment = (int)SpriteAlignment.Custom;
                settings.spritePivot = pivot;
                importer.SetTextureSettings(settings);
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = ppu;
                importer.SaveAndReimport();
            }
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null) throw new InvalidOperationException("Sprite import failed: " + path);
            return sprite;
        }

        private static Material EnsureMaterial(string path, Texture2D texture)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) return material;
            Shader shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
            if (shader == null) throw new InvalidOperationException("URP 2D Sprite-Unlit shader is missing");
            material = new Material(shader) { name = Path.GetFileNameWithoutExtension(path), mainTexture = texture };
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static Material MaterialFor(Sprite sprite)
        {
            string sourcePath = AssetDatabase.GetAssetPath(sprite);
            string path = sourcePath.Substring(0, sourcePath.LastIndexOf('.')) + "_Unlit.mat";
            return EnsureMaterial(path, sprite.texture);
        }

        private static void ApplyPlayerPrefab(AnimatorController controller)
        {
            string path = Root + "/Prefabs/ZhaoYun.prefab";
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            bool changed = false;
            try
            {
                Transform art = root.transform.Find("Animated SE");
                if (art == null)
                {
                    art = new GameObject("Animated SE").transform;
                    art.SetParent(root.transform, false);
                    art.gameObject.AddComponent<SpriteRenderer>();
                    art.gameObject.AddComponent<WorldYSort>();
                    art.gameObject.AddComponent<Animator>();
                    changed = true;
                }
                var renderer = art.GetComponent<SpriteRenderer>();
                if (renderer.sprite != Pose("Idle_A")) { renderer.sprite = Pose("Idle_A"); changed = true; }
                if (renderer.sharedMaterial != characterMaterial) { renderer.sharedMaterial = characterMaterial; changed = true; }
                if (art.localScale != Vector3.one) { art.localScale = Vector3.one; changed = true; }
                var animator = art.GetComponent<Animator>();
                if (animator.runtimeAnimatorController != controller) { animator.runtimeAnimatorController = controller; changed = true; }
                var sort = art.GetComponent<WorldYSort>();
                if (sort.sortingOffset != 5) { sort.sortingOffset = 5; changed = true; }
                Transform old = root.transform.Find("Static SE Prototype");
                if (old != null && old.GetComponent<SpriteRenderer>()?.enabled == true)
                { old.GetComponent<SpriteRenderer>().enabled = false; changed = true; }
                var visual = root.GetComponent<PlayerVisualAnimator>();
                if (visual == null) { visual = root.AddComponent<PlayerVisualAnimator>(); changed = true; }
                if (visual.Target != animator || visual.CharacterRenderer != renderer ||
                    visual.IdleMaterial != characterMaterial || visual.AttackMaterial != attackMaterial ||
                    visual.NorthAttackMaterial != verticalAttackMaterial || visual.SouthAttackMaterial != verticalAttackMaterial ||
                    visual.NorthWalkMaterial != northWalkMaterial || visual.SouthWalkMaterial != southWalkMaterial ||
                    visual.ComboAttackMaterial != comboMaterial || visual.NorthComboMaterial != northComboMaterial ||
                    visual.SouthComboMaterial != southComboMaterial)
                {
                    visual.Configure(animator, renderer, characterMaterial, attackMaterial,
                        verticalAttackMaterial, verticalAttackMaterial,
                        northWalkMaterial, southWalkMaterial, comboMaterial,
                        northComboMaterial, southComboMaterial);
                    changed = true;
                }
                if (root.GetComponent<PlayerAttackVfx>() == null)
                { root.AddComponent<PlayerAttackVfx>(); changed = true; }
                if (EnsureShadow(root.transform, Vector3.one)) changed = true;
                Transform indicator = root.transform.Find("Direction Indicator");
                if (indicator != null)
                {
                    var indicatorRenderer = indicator.GetComponent<SpriteRenderer>();
                    if (indicatorRenderer != null && indicatorRenderer.enabled)
                    { indicatorRenderer.enabled = false; changed = true; }
                }
                Transform trail = root.transform.Find("Spear Trail");
                if (trail != null && trail.GetComponent<SpriteRenderer>()?.enabled == true)
                { trail.GetComponent<SpriteRenderer>().enabled = false; changed = true; }
                if (changed) PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        private static void ApplyEnemyPrefab(string label, Sprite idle, Sprite warning, Sprite strike, Sprite recover)
        {
            string path = Root + "/Prefabs/" + label + ".prefab";
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            bool changed = false;
            try
            {
                Transform art = root.transform.Find("Generated Visual");
                if (art == null)
                {
                    art = new GameObject("Generated Visual").transform;
                    art.SetParent(root.transform, false);
                    art.gameObject.AddComponent<SpriteRenderer>();
                    art.gameObject.AddComponent<WorldYSort>();
                    art.gameObject.AddComponent<EnemyVisual>();
                    changed = true;
                }
                Vector3 inverseScale = new Vector3(1f / root.transform.localScale.x, 1f / root.transform.localScale.y, 1);
                if (art.localScale != inverseScale) { art.localScale = inverseScale; changed = true; }
                var renderer = art.GetComponent<SpriteRenderer>();
                if (renderer.sprite != idle) { renderer.sprite = idle; changed = true; }
                Material material = MaterialFor(idle);
                if (renderer.sharedMaterial != material) { renderer.sharedMaterial = material; changed = true; }
                Material warningMaterial = warning != null ? MaterialFor(warning) : material;
                Material strikeMaterial = strike != null ? MaterialFor(strike) : warningMaterial;
                Material recoverMaterial = recover != null ? MaterialFor(recover) : material;
                var visual = art.GetComponent<EnemyVisual>();
                if (visual.Idle != idle || visual.Warning != warning || visual.Strike != strike || visual.Recover != recover ||
                    visual.IdleMaterial != material || visual.WarningMaterial != warningMaterial ||
                    visual.StrikeMaterial != strikeMaterial || visual.RecoverMaterial != recoverMaterial)
                {
                    visual.Configure(idle, warning, strike, recover, material, warningMaterial, strikeMaterial, recoverMaterial);
                    changed = true;
                }
                var sort = art.GetComponent<WorldYSort>();
                if (sort.sortingOffset != 5) { sort.sortingOffset = 5; changed = true; }
                var placeholder = root.GetComponent<SpriteRenderer>();
                if (placeholder != null && placeholder.enabled) { placeholder.enabled = false; changed = true; }
                if (EnsureShadow(root.transform, inverseScale)) changed = true;
                if (changed) PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        private static bool EnsureShadow(Transform parent, Vector3 inverseScale)
        {
            if (parent.Find("Generated Shadow") != null) return false;
            var shadow = new GameObject("Generated Shadow");
            shadow.transform.SetParent(parent, false);
            shadow.transform.localPosition = new Vector3(0, -0.03f, 0);
            shadow.transform.localScale = new Vector3(1.05f * inverseScale.x, 0.22f * inverseScale.y, 1);
            var renderer = shadow.AddComponent<SpriteRenderer>();
            renderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(Root + "/Art/Generated/Disc.png");
            renderer.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(Root + "/Art/Generated/SpriteUnlit.mat");
            renderer.color = new Color(0.05f, 0.06f, 0.05f, 0.28f);
            shadow.AddComponent<WorldYSort>().sortingOffset = -20;
            return true;
        }

        private static void ApplyMainScene(Sprite house, Sprite cart, Sprite ground, Sprite continuousGround,
            Sprite rescue, Sprite exit, Sprite fenceHorizontal, Sprite fenceVertical)
        {
            Scene scene = EditorSceneManager.OpenScene(MainScene);
            bool changed = false;
            GameObject artRoot = GameObject.Find("Art Pass — Environment");
            if (artRoot == null) { artRoot = new GameObject("Art Pass — Environment"); changed = true; }
            Material houseMaterial = MaterialFor(house);
            Material cartMaterial = MaterialFor(cart);
            Material groundMaterial = MaterialFor(ground);
            Material continuousGroundMaterial = MaterialFor(continuousGround);
            GameBalance balance = AssetDatabase.LoadAssetAtPath<GameBalance>(Root + "/Core/GameBalance.asset");
            if (balance == null) throw new InvalidOperationException("GameBalance asset missing");
            Vector2 groundMin = new Vector2(-28f, -9f);
            Vector2 groundMax = new Vector2(37f, 9f);
            if (balance.visibleGroundMin != groundMin || balance.visibleGroundMax != groundMax)
            { balance.visibleGroundMin = groundMin; balance.visibleGroundMax = groundMax; EditorUtility.SetDirty(balance); }
            if (Mathf.Approximately(balance.attackReach, 1.75f))
            { balance.attackReach = 1.45f; EditorUtility.SetDirty(balance); }
            if (balance.attackWidth <= 0f || balance.attackAimDot <= 0f || balance.attackStep <= 0f)
            { balance.attackWidth = 0.72f; balance.attackAimDot = 0.55f; balance.attackStep = 0.16f; EditorUtility.SetDirty(balance); }
            CameraFollow follow = Camera.main != null ? Camera.main.GetComponent<CameraFollow>() : null;
            if (follow != null && follow.balance != balance) { follow.balance = balance; changed = true; }
            Transform continuous = artRoot.transform.Find("Continuous Ground");
            if (continuous == null)
            {
                continuous = new GameObject("Continuous Ground").transform;
                continuous.SetParent(artRoot.transform, false);
                continuous.gameObject.AddComponent<SpriteRenderer>();
                changed = true;
            }
            SpriteRenderer fullGround = continuous.GetComponent<SpriteRenderer>();
            Vector3 groundCenter = new Vector3((groundMin.x + groundMax.x) * 0.5f, (groundMin.y + groundMax.y) * 0.5f, 0f);
            Vector2 groundSize = groundMax - groundMin;
            Color groundTint = new Color(0.77f, 0.74f, 0.69f, 1f);
            if (fullGround.sprite != continuousGround || fullGround.sharedMaterial != continuousGroundMaterial ||
                fullGround.drawMode != SpriteDrawMode.Tiled || fullGround.size != groundSize ||
                fullGround.color != groundTint || fullGround.sortingOrder != -650 ||
                continuous.position != groundCenter || continuous.localScale != Vector3.one)
            {
                fullGround.sprite = continuousGround;
                fullGround.sharedMaterial = continuousGroundMaterial;
                fullGround.drawMode = SpriteDrawMode.Tiled;
                fullGround.size = groundSize;
                fullGround.color = groundTint;
                fullGround.sortingOrder = -650;
                continuous.position = groundCenter;
                continuous.localScale = Vector3.one;
                changed = true;
            }
            foreach (string name in new[] { "Village ground", "Ruined camp ground", "Evacuation ground" })
            {
                SpriteRenderer oldZone = GameObject.Find(name)?.GetComponent<SpriteRenderer>();
                if (oldZone != null && oldZone.enabled) { oldZone.enabled = false; changed = true; }
            }
            for (int i = 0; i < 4; i++)
                changed |= PlaceArt(artRoot.transform, "Road texture " + i, ground, groundMaterial,
                    new Vector2(-10 + i * 10, 0), 1f, -480, new Color(0.74f, 0.73f, 0.67f, 0.14f));
            for (int i = 0; i < 4; i++)
            {
                Transform patch = artRoot.transform.Find("Road texture " + i);
                if (patch != null && patch.GetComponent<SpriteRenderer>().enabled)
                { patch.GetComponent<SpriteRenderer>().enabled = false; changed = true; }
            }
            GameObject roadObject = GameObject.Find("Road");
            if (roadObject != null)
            {
                var roadRenderer = roadObject.GetComponent<SpriteRenderer>();
                if (roadRenderer.sprite != ground || roadRenderer.drawMode != SpriteDrawMode.Tiled ||
                    roadRenderer.size != new Vector2(45, 3) || roadObject.transform.localScale != Vector3.one ||
                    roadRenderer.color != new Color(0.67f, 0.61f, 0.52f, 0.24f))
                {
                    roadObject.transform.localScale = Vector3.one;
                    roadRenderer.sprite = ground;
                    roadRenderer.drawMode = SpriteDrawMode.Tiled;
                    roadRenderer.size = new Vector2(45, 3);
                    roadRenderer.sharedMaterial = groundMaterial;
                    roadRenderer.color = new Color(0.67f, 0.61f, 0.52f, 0.24f);
                    roadRenderer.sortingOrder = -440;
                    roadObject.transform.localScale = Vector3.one;
                    changed = true;
                }
            }
            foreach (string name in new[] { "Ruined wall A", "Ruined wall B" })
            {
                GameObject placeholder = GameObject.Find(name);
                if (placeholder != null && placeholder.GetComponent<SpriteRenderer>()?.enabled == true)
                { placeholder.GetComponent<SpriteRenderer>().enabled = false; changed = true; }
            }
            foreach (string name in new[] { "North wall", "South wall" })
            {
                GameObject edge = GameObject.Find(name);
                if (edge == null) continue;
                var edgeRenderer = edge.GetComponent<SpriteRenderer>();
                if (edgeRenderer != null && edgeRenderer.enabled)
                { edgeRenderer.enabled = false; changed = true; }
            }
            changed |= ApplyVisibleBoundary(artRoot.transform, balance, fenceHorizontal, fenceVertical);
            changed |= PlaceArt(artRoot.transform, "Ruined house at village", house, houseMaterial,
                new Vector2(-7.0f, 2.8f), 0.95f, -285, Color.white);
            changed |= PlaceArt(artRoot.transform, "Ruined house at camp", house, houseMaterial,
                new Vector2(8.0f, 2.9f), 0.85f, -295, new Color(0.83f, 0.82f, 0.76f));
            changed |= PlaceArt(artRoot.transform, "Broken cart at village", cart, cartMaterial,
                new Vector2(-2.0f, -2.6f), 0.9f, 245, Color.white);
            changed |= PlaceArt(artRoot.transform, "Broken cart at exit", cart, cartMaterial,
                new Vector2(13f, 2.8f), 0.75f, -280, new Color(0.83f, 0.83f, 0.80f));
            GameObject rescueObject = GameObject.Find("A Dou — Press E");
            if (rescueObject != null)
            {
                var renderer = rescueObject.GetComponent<SpriteRenderer>();
                if (renderer != null && renderer.sprite != rescue)
                { renderer.sprite = rescue; renderer.sharedMaterial = MaterialFor(rescue); renderer.color = Color.white; changed = true; }
                Transform aura = rescueObject.transform.Find("Rescue Aura");
                if (aura == null)
                {
                    aura = new GameObject("Rescue Aura").transform;
                    aura.SetParent(rescueObject.transform, false);
                    aura.localPosition = new Vector3(0, -0.12f, 0);
                    aura.localScale = new Vector3(2.0f, 0.62f, 1);
                    var auraRenderer = aura.gameObject.AddComponent<SpriteRenderer>();
                    auraRenderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(Root + "/Art/Generated/Disc.png");
                    auraRenderer.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(Root + "/Art/Generated/SpriteUnlit.mat");
                    auraRenderer.color = new Color(0.91f, 0.70f, 0.28f, 0.42f);
                    auraRenderer.sortingOrder = -65;
                    changed = true;
                }
            }
            GameObject exitObject = GameObject.Find("Evacuation marker");
            if (exitObject != null)
            {
                var renderer = exitObject.GetComponent<SpriteRenderer>();
                if (renderer != null && renderer.sprite != exit)
                {
                    renderer.sprite = exit; renderer.sharedMaterial = MaterialFor(exit); renderer.color = Color.white;
                    exitObject.transform.localScale = Vector3.one;
                    changed = true;
                }
            }
            if (changed) EditorSceneManager.SaveScene(scene);
        }

        private static bool ApplyVisibleBoundary(Transform parent, GameBalance balance,
            Sprite horizontal, Sprite vertical)
        {
            bool changed = false;
            Material horizontalMaterial = MaterialFor(horizontal);
            Material verticalMaterial = MaterialFor(vertical);
            const float stepX = 3.75f;
            int countX = Mathf.CeilToInt((balance.playableMax.x - balance.playableMin.x) / stepX) + 1;
            for (int i = 0; i < countX; i++)
            {
                float x = balance.playableMin.x + 1.45f + i * stepX;
                changed |= PlaceBoundary(parent, "Palisade North " + i.ToString("00"), horizontal,
                    horizontalMaterial, new Vector2(x, balance.playableMax.y + 0.3f), 1f);
                changed |= PlaceBoundary(parent, "Palisade South " + i.ToString("00"), horizontal,
                    horizontalMaterial, new Vector2(x, balance.playableMin.y - 0.95f), 1f);
            }
            for (int i = 0; i < 4; i++)
            {
                float y = -2.65f + i * 1.77f;
                changed |= PlaceBoundary(parent, "Palisade West " + i.ToString("00"), vertical,
                    verticalMaterial, new Vector2(balance.playableMin.x - 0.48f, y), 0.62f);
                changed |= PlaceBoundary(parent, "Palisade East " + i.ToString("00"), vertical,
                    verticalMaterial, new Vector2(balance.playableMax.x + 0.48f, y), 0.62f);
            }
            return changed;
        }

        private static bool PlaceBoundary(Transform parent, string name, Sprite sprite, Material material,
            Vector2 position, float scale)
        {
            Transform item = parent.Find(name);
            bool changed = false;
            if (item == null)
            {
                item = new GameObject(name).transform;
                item.SetParent(parent, false);
                item.gameObject.AddComponent<SpriteRenderer>();
                item.gameObject.AddComponent<WorldYSort>();
                changed = true;
            }
            var renderer = item.GetComponent<SpriteRenderer>();
            if (renderer.sprite != sprite) { renderer.sprite = sprite; changed = true; }
            if (renderer.sharedMaterial != material) { renderer.sharedMaterial = material; changed = true; }
            if (renderer.color != Color.white) { renderer.color = Color.white; changed = true; }
            if (item.position != (Vector3)position) { item.position = position; changed = true; }
            if (item.localScale != Vector3.one * scale) { item.localScale = Vector3.one * scale; changed = true; }
            return changed;
        }

        private static bool PlaceArt(Transform parent, string name, Sprite sprite, Material material,
            Vector2 position, float scale, int order, Color tint)
        {
            Transform existing = parent.Find(name);
            if (existing != null)
            {
                var current = existing.GetComponent<SpriteRenderer>();
                bool changed = false;
                if (current.sprite != sprite) { current.sprite = sprite; changed = true; }
                if (current.sharedMaterial != material) { current.sharedMaterial = material; changed = true; }
                if (current.color != tint) { current.color = tint; changed = true; }
                return changed;
            }
            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            obj.transform.position = position;
            obj.transform.localScale = Vector3.one * scale;
            var renderer = obj.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sharedMaterial = material;
            renderer.color = tint;
            renderer.sortingOrder = order;
            return true;
        }

        private static void ApplyArtScene()
        {
            Scene scene = EditorSceneManager.OpenScene(ArtScene);
            bool changed = false;
            GameObject old = GameObject.Find("Zhao Yun static prototype");
            var renderer = old != null ? old.GetComponent<SpriteRenderer>() : null;
            if (renderer == null) throw new InvalidOperationException("Art Validation character missing");
            if (renderer.sprite != Pose("Idle_A")) { renderer.sprite = Pose("Idle_A"); changed = true; }
            if (renderer.sharedMaterial != characterMaterial) { renderer.sharedMaterial = characterMaterial; changed = true; }
            var controls = UnityEngine.Object.FindFirstObjectByType<ArtValidationControls>();
            Sprite[] ordered = poses.Values.Concat(attackPoses.Values).Concat(verticalAttackPoses.Values)
                .Concat(northWalkPoses.Values).Concat(southWalkPoses.Values)
                .Concat(comboPoses.Values.Where(sprite => sprite.name.Contains("Combo")))
                .Concat(northComboPoses.Values.Where(sprite => sprite.name.Contains("Combo")))
                .Concat(southComboPoses.Values.Where(sprite => sprite.name.Contains("Combo")))
                .OrderBy(s => s.name).ToArray();
            if (controls != null && (controls.poses == null || !controls.poses.SequenceEqual(ordered)))
            { controls.poses = ordered; changed = true; }
            if (controls != null && (controls.idleMaterial != characterMaterial || controls.attackMaterial != attackMaterial))
            { controls.idleMaterial = characterMaterial; controls.attackMaterial = attackMaterial; changed = true; }
            if (controls != null && controls.verticalAttackMaterial != verticalAttackMaterial)
            { controls.verticalAttackMaterial = verticalAttackMaterial; changed = true; }
            if (controls != null && (controls.northWalkMaterial != northWalkMaterial ||
                controls.southWalkMaterial != southWalkMaterial || controls.comboMaterial != comboMaterial ||
                controls.northComboMaterial != northComboMaterial || controls.southComboMaterial != southComboMaterial))
            {
                controls.northWalkMaterial = northWalkMaterial;
                controls.southWalkMaterial = southWalkMaterial;
                controls.comboMaterial = comboMaterial;
                controls.northComboMaterial = northComboMaterial;
                controls.southComboMaterial = southComboMaterial;
                changed = true;
            }
            if (changed) EditorSceneManager.SaveScene(scene);
        }
    }
}
