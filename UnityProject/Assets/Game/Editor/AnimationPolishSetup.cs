using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEditor.U2D.Sprites;
using UnityEngine;

namespace Changban.EditorTools
{
    // A small versioned migration, separate from the initial scene builder and art import pass.
    public static class AnimationPolishSetup
    {
        private const string Anim = "Assets/Game/Art/Characters/ZhaoYun/Animations";
        private const string Sheet = "Assets/Game/Art/Characters/ZhaoYun/AnimationSources/RunEastV4/ZY_E_RunV4_Sheet.png";

        [MenuItem("Changban/Apply Animation Polish")]
        public static void Apply()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            ApplyToCurrentArt();
            EditorSceneManager.OpenScene("Assets/Game/Scenes/ChangbanDemo.unity");
            Debug.Log("Animation polish V4 applied: grounded idle, six-frame horizontal run, ready stances and enemy timing.");
        }

        public static void ApplyToCurrentArt()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            var original = AssetDatabase.LoadAllAssetsAtPath(Anim + "/ZhaoYunSEAtlas.asset")
                .OfType<Sprite>().First(s => s.name == "ZY_SE_Idle_A");
            string idlePath = Anim + "/ZhaoYunGroundedIdleV4.asset";
            Sprite grounded = AssetDatabase.LoadAssetAtPath<Sprite>(idlePath);
            if (grounded == null)
            {
                // Measured between the two boot soles in the original PNG; no alpha/colour edits.
                grounded = Sprite.Create(original.texture, original.rect, new Vector2(0.46f, 0.12f),
                    original.pixelsPerUnit, 0, SpriteMeshType.FullRect);
                grounded.name = "ZY_SE_IdleGroundedV4";
                AssetDatabase.CreateAsset(grounded, idlePath);
            }

            Sprite[] frames = EnsureRunFrames(original.bounds.size.y * 0.94f);
            Texture2D sheet = AssetDatabase.LoadAssetAtPath<Texture2D>(Sheet);
            string materialPath = Anim + "/ZhaoYunRunV4Unlit.mat";
            Material runMaterial = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (runMaterial == null)
            {
                runMaterial = new Material(Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default"))
                    { name = "Zhao Yun Run V4", mainTexture = sheet };
                AssetDatabase.CreateAsset(runMaterial, materialPath);
            }
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(Anim + "/ZhaoYunSE.controller");
            WriteState(controller, "Idle", new[] { grounded, grounded }, new[] { 0f, 0.8f }, true);
            Sprite[] cycle = frames.Concat(new[] { frames[0] }).ToArray();
            WriteState(controller, "Walk", cycle, new[] { 0f, .08f, .16f, .24f, .32f, .40f, .48f }, true);
            Sprite recovery = AssetDatabase.LoadAllAssetsAtPath(Anim + "/ZhaoYunAttackV2Atlas.asset")
                .OfType<Sprite>().First(s => s.name == "ZY_SE_AttackV2_Recovery");
            WriteState(controller, "Ready", new[] { recovery, recovery }, new[] { 0f, .2f }, true);
            var vertical = AssetDatabase.LoadAllAssetsAtPath(Anim + "/ZhaoYunVerticalAttackV2Atlas.asset").OfType<Sprite>().ToArray();
            foreach (string direction in new[] { "N", "S" })
            {
                Sprite pose = vertical.First(s => s.name == "ZY_" + direction + "_AttackV2_Recovery");
                WriteState(controller, direction == "N" ? "ReadyNorth" : "ReadySouth",
                    new[] { pose, pose }, new[] { 0f, .2f }, true);
            }
            string prefabPath = "Assets/Game/Prefabs/ZhaoYun.prefab";
            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                PlayerVisualAnimator visual = root.GetComponent<PlayerVisualAnimator>();
                visual.ConfigureLocomotion(runMaterial, visual.AttackMaterial);
                visual.CharacterRenderer.sprite = grounded;
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }

            GameBalance balance = AssetDatabase.LoadAssetAtPath<GameBalance>("Assets/Game/Core/GameBalance.asset");
            // Only migrate the old default. Preserve already customized recovery speeds.
            if (Mathf.Approximately(balance.enemyRecovery, 0.8f))
            {
                balance.enemyRecovery = 0.18f;
                EditorUtility.SetDirty(balance);
            }
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            UpdateArtValidation(frames, runMaterial, grounded);
            AssetDatabase.SaveAssets();
        }

        private static Sprite[] EnsureRunFrames(float targetHeight)
        {
            var importer = AssetImporter.GetAtPath(Sheet) as TextureImporter;
            if (importer == null) throw new FileNotFoundException("Run V4 sheet is missing", Sheet);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = 337f / targetHeight;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 2048;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.Tight;
            settings.spriteExtrude = 0;
            importer.SetTextureSettings(settings);
            var factory = new SpriteDataProviderFactories();
            factory.Init();
            var provider = factory.GetSpriteEditorDataProviderFromObject(importer);
            provider.InitSpriteEditorDataProvider();
            SpriteRect[] previous = provider.GetSpriteRects();
            var rects = new SpriteRect[6];
            int[] rowTop = { 0, 368, 690 };
            int[] rowBottom = { 368, 704, 1024 };
            float[] groundY = { 358f, 329f, 329f };
            float[] hipX = { 430f, 420f, 420f, 430f, 410f, 427f };
            for (int i = 0; i < 6; i++)
            {
                int row = i / 2;
                int height = rowBottom[row] - rowTop[row];
                string name = "ZY_E_RunV4_" + (i + 1).ToString("00");
                rects[i] = new SpriteRect {
                    name = name,
                    spriteID = previous.FirstOrDefault(s => s.name == name)?.spriteID ?? GUID.Generate(),
                    rect = new Rect((i % 2) * 768, 1024 - rowBottom[row], 768, height),
                    alignment = SpriteAlignment.Custom,
                    pivot = new Vector2(hipX[i] / 768f, (height - groundY[row]) / height)
                };
            }
            provider.SetSpriteRects(rects);
            provider.GetDataProvider<ISpriteNameFileIdDataProvider>().SetNameFileIdPairs(
                rects.Select(s => new SpriteNameFileIdPair(s.name, s.spriteID)));
            var outline = provider.GetDataProvider<ISpriteOutlineDataProvider>();
            for (int i = 0; i < 6; i++)
            {
                float h = rects[i].rect.height;
                Vector2[] points = i == 2
                    ? new[] { new Vector2(0,14), new Vector2(0,h), new Vector2(768,h), new Vector2(768,14) }
                    : i == 3
                        ? new[] { new Vector2(0,0), new Vector2(0,h), new Vector2(768,h), new Vector2(768,0),
                            new Vector2(480,0), new Vector2(480,14), new Vector2(220,14), new Vector2(220,0) }
                    : i >= 4
                        ? new[] { new Vector2(0,0), new Vector2(0,320), new Vector2(400,320), new Vector2(400,h),
                            new Vector2(i == 4 ? 510 : 470,h), new Vector2(i == 4 ? 510 : 470,320),
                            new Vector2(768,320), new Vector2(768,0) }
                        : new[] { new Vector2(0,0), new Vector2(0,h), new Vector2(768,h), new Vector2(768,0) };
                Vector2 centre = rects[i].rect.size * 0.5f;
                outline.SetOutlines(rects[i].spriteID, new List<Vector2[]> { points.Select(p => p - centre).ToArray() });
            }
            provider.Apply();
            importer.SaveAndReimport();
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(Sheet);
            if (texture.width != 1536 || texture.height != 1024)
                throw new InvalidOperationException("The measured V4 source is a 1536 x 1024 sheet.");
            var imported = AssetDatabase.LoadAllAssetsAtPath(Sheet).OfType<Sprite>().ToArray();
            var frames = new Sprite[6];
            for (int i = 0; i < 6; i++)
            {
                Sprite source = imported.First(s => s.name == rects[i].name);
                string path = Anim + "/" + source.name + ".asset";
                frames[i] = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (frames[i] == null)
                {
                    frames[i] = UnityEngine.Object.Instantiate(source);
                    frames[i].name = source.name;
                    AssetDatabase.CreateAsset(frames[i], path);
                }
                else
                {
                    EditorUtility.CopySerialized(source, frames[i]);
                    EditorUtility.SetDirty(frames[i]);
                }
            }
            return frames;
        }

        public static void VerifyAndBuild()
        {
            Apply();
            var paths = AssetDatabase.FindAssets("", new[] { Anim }).Select(AssetDatabase.GUIDToAssetPath)
                .Where(File.Exists).Concat(new[] { "Assets/Game/Prefabs/ZhaoYun.prefab",
                    "Assets/Game/Core/GameBalance.asset", "Assets/Game/Scenes/Tests/ArtValidation.unity" }).ToArray();
            var before = paths.ToDictionary(p => p, Hash);
            const string artScene = "Assets/Game/Scenes/Tests/ArtValidation.unity";
            string sceneBefore = File.ReadAllText(artScene);
            Apply();
            foreach (string path in paths)
                if (before[path] != Hash(path))
                {
                    Directory.CreateDirectory("Logs");
                    File.WriteAllText("Logs/polish-artscene-before.txt", sceneBefore);
                    File.WriteAllText("Logs/polish-artscene-after.txt", File.ReadAllText(artScene));
                    throw new InvalidOperationException("Repeated polish changed " + path);
                }
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(Anim + "/ZhaoYunSE.controller");
            if (controller.layers[0].stateMachine.states.GroupBy(s => s.state.name).Any(g => g.Count() > 1))
                throw new InvalidOperationException("Duplicate Animator states after repeated polish.");
            var sprites = AssetDatabase.FindAssets("t:Sprite", new[] { Anim })
                .Select(AssetDatabase.GUIDToAssetPath).Select(AssetDatabase.LoadAssetAtPath<Sprite>)
                .Count(s => s != null && s.name.StartsWith("ZY_E_RunV4_"));
            if (sprites != 6) throw new InvalidOperationException("Expected exactly six Run V4 sprites, found " + sprites);
            Debug.Log("Animation polish repeat check passed: stable asset hashes, unique states, six run sprites.");
            DemoVerification.ValidateAssets();
            DemoVerification.BuildWindows();
        }

        private static string Hash(string path)
        {
            using (var digest = SHA256.Create()) return Convert.ToBase64String(digest.ComputeHash(File.ReadAllBytes(path)));
        }

        private static void WriteState(AnimatorController controller, string name, Sprite[] frames, float[] times, bool loop)
        {
            string path = Anim + "/" + name + ".anim";
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (clip == null)
            {
                clip = new AnimationClip { name = name, frameRate = 30f };
                AssetDatabase.CreateAsset(clip, path);
            }
            var binding = new EditorCurveBinding { path = "", type = typeof(SpriteRenderer), propertyName = "m_Sprite" };
            var keys = new ObjectReferenceKeyframe[frames.Length];
            for (int i = 0; i < frames.Length; i++) keys[i] = new ObjectReferenceKeyframe { time = times[i], value = frames[i] };
            AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = loop;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            AnimatorStateMachine machine = controller.layers[0].stateMachine;
            AnimatorState state = machine.states.Select(s => s.state).FirstOrDefault(s => s.name == name);
            if (state == null) state = machine.AddState(name);
            state.motion = clip;
            EditorUtility.SetDirty(clip);
        }

        private static void UpdateArtValidation(Sprite[] frames, Material runMaterial, Sprite grounded)
        {
            var scene = EditorSceneManager.OpenScene("Assets/Game/Scenes/Tests/ArtValidation.unity");
            var controls = UnityEngine.Object.FindAnyObjectByType<ArtValidationControls>();
            if (controls == null) throw new InvalidOperationException("ArtValidation controls are missing.");
            Sprite[] poses = controls.poses.Where(s => s != null && !s.name.StartsWith("ZY_E_RunV4_") &&
                s != grounded).Concat(frames).Append(grounded).ToArray();
            if (controls.runMaterial != runMaterial || !controls.poses.SequenceEqual(poses))
            {
                controls.runMaterial = runMaterial;
                controls.poses = poses;
                EditorUtility.SetDirty(controls);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
        }
    }
}
