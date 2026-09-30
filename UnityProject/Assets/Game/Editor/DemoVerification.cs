using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Changban.EditorTools
{
    public static class DemoVerification
    {
        private const string Main = "Assets/Game/Scenes/ChangbanDemo.unity";
        private const string Art = "Assets/Game/Scenes/Tests/ArtValidation.unity";

        public static void ValidateAssets()
        {
            int missing = 0;
            foreach (string scenePath in new[] { Main, Art })
            {
                var scene = EditorSceneManager.OpenScene(scenePath);
                foreach (GameObject root in scene.GetRootGameObjects())
                    foreach (Transform node in root.GetComponentsInChildren<Transform>(true))
                        missing += GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(node.gameObject);
            }
            foreach (string name in new[] { "Arrow", "Melee", "Archer", "Elite", "ZhaoYun" })
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/" + name + ".prefab");
                if (prefab == null) throw new InvalidOperationException("Prefab missing: " + name);
                foreach (Transform node in prefab.GetComponentsInChildren<Transform>(true))
                    missing += GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(node.gameObject);
            }
            if (missing != 0) throw new InvalidOperationException("Missing scripts in generated assets: " + missing);
            Debug.Log("Validated 2 scenes and 5 prefabs: zero Missing Script components.");
        }

        public static void CaptureScenes()
        {
            string folder = Path.Combine(Application.dataPath, "../Docs/Screenshots");
            Directory.CreateDirectory(folder);
            EditorSceneManager.OpenScene(Main);
            Camera camera = Camera.main;
            camera.transform.position = new Vector3(-9, 0, -10);
            Capture(camera, Path.Combine(folder, "ChangbanVillage.png"));
            camera.transform.position = new Vector3(6, 0, -10);
            Capture(camera, Path.Combine(folder, "ChangbanRescue.png"));
            camera.transform.position = new Vector3(18, 0, -10);
            Capture(camera, Path.Combine(folder, "ChangbanExit.png"));
            Sprite attackPose = AssetDatabase.LoadAllAssetsAtPath(
                    "Assets/Game/Art/Characters/ZhaoYun/Animations/ZhaoYunAttackV2Atlas.asset")
                .OfType<Sprite>().FirstOrDefault(s => s.name == "ZY_SE_AttackV2_Extend");
            PlayerVisualAnimator player = UnityEngine.Object.FindAnyObjectByType<PlayerVisualAnimator>();
            EnemyVisual enemy = UnityEngine.Object.FindAnyObjectByType<EnemyVisual>();
            if (attackPose != null && player != null && enemy != null && player.CharacterRenderer != null)
            {
                player.CharacterRenderer.sprite = attackPose;
                player.CharacterRenderer.sharedMaterial = player.AttackMaterial;
                enemy.transform.parent.position = player.transform.position + Vector3.right * 1.15f;
                SpriteRenderer enemyGraphic = enemy.GetComponent<SpriteRenderer>();
                enemyGraphic.sprite = enemy.Strike;
                enemyGraphic.sharedMaterial = enemy.StrikeMaterial;
                camera.transform.position = new Vector3(player.transform.position.x + 0.55f,
                    player.transform.position.y, -10);
                camera.orthographicSize = 2.7f;
                Capture(camera, Path.Combine(folder, "ChangbanCombat.png"));
            }
            EditorSceneManager.OpenScene(Art);
            camera = Camera.main;
            camera.orthographicSize = 4.2f;
            camera.backgroundColor = new Color(0.72f, 0.72f, 0.71f);
            Capture(camera, Path.Combine(folder, "ArtValidationLight.png"));
            camera.backgroundColor = new Color(0.17f, 0.19f, 0.20f);
            Capture(camera, Path.Combine(folder, "ArtValidationDark.png"));
            camera.backgroundColor = new Color(0.43f, 0.40f, 0.35f);
            Capture(camera, Path.Combine(folder, "ArtValidationTaupe.png"));
            var controls = UnityEngine.Object.FindAnyObjectByType<ArtValidationControls>();
            if (attackPose != null && controls != null && controls.character != null)
            {
                controls.character.sprite = attackPose;
                if (controls.attackMaterial != null) controls.character.sharedMaterial = controls.attackMaterial;
                camera.backgroundColor = new Color(0.72f, 0.72f, 0.71f);
                Capture(camera, Path.Combine(folder, "ArtValidationThrust.png"));
            }
            Sprite[] vertical = AssetDatabase.LoadAllAssetsAtPath(
                    "Assets/Game/Art/Characters/ZhaoYun/Animations/ZhaoYunVerticalAttackV2Atlas.asset")
                .OfType<Sprite>().ToArray();
            if (controls != null && controls.character != null && controls.verticalAttackMaterial != null)
            {
                controls.character.sharedMaterial = controls.verticalAttackMaterial;
                Sprite north = vertical.FirstOrDefault(s => s.name == "ZY_N_AttackV2_Extend");
                Sprite south = vertical.FirstOrDefault(s => s.name == "ZY_S_AttackV2_Extend");
                if (north != null)
                {
                    controls.character.sprite = north;
                    Capture(camera, Path.Combine(folder, "ArtValidationNorthThrust.png"));
                }
                if (south != null)
                {
                    controls.character.sprite = south;
                    Capture(camera, Path.Combine(folder, "ArtValidationSouthThrust.png"));
                }
            }
            Debug.Log("Captured actual Unity camera renders in " + folder);
        }

        private static void Capture(Camera camera, string path)
        {
            if (camera == null) throw new InvalidOperationException("Scene camera missing: " + path);
            RenderTexture render = RenderTexture.GetTemporary(1280, 720, 24, RenderTextureFormat.ARGB32);
            RenderTexture previous = RenderTexture.active;
            RenderTexture previousTarget = camera.targetTexture;
            Texture2D image = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            try
            {
                camera.targetTexture = render;
                camera.Render();
                RenderTexture.active = render;
                image.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
                image.Apply();
                File.WriteAllBytes(path, image.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = previousTarget;
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(render);
                UnityEngine.Object.DestroyImmediate(image);
            }
        }

        public static void BuildWindows()
        {
            string output = Path.GetFullPath(Path.Combine(Application.dataPath, "../Builds/Windows/ChangbanDemo.exe"));
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { Main },
                locationPathName = output,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            });
            Debug.Log("Windows build result: " + report.summary.result + ", errors: " + report.summary.totalErrors + ", output: " + output);
            if (report.summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException("Windows build did not succeed: " + report.summary.result);
        }
    }
}
