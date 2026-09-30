using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Changban.EditorTools
{
    public static class DemoSetup
    {
        private const string Root = "Assets/Game";
        private const string MainScene = Root + "/Scenes/ChangbanDemo.unity";
        private const string ArtScene = Root + "/Scenes/Tests/ArtValidation.unity";
        private static Sprite pixel;
        private static Sprite disc;
        private static Material unlit;
        private static Material characterUnlit;
        private static GameBalance balance;

        [MenuItem("Changban/Setup Demo")]
        public static void SetupDemo()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EnsureFolders();
            EnsureImportedArt();
            pixel = EnsureGeneratedSprite("Pixel", false);
            disc = EnsureGeneratedSprite("Disc", true);
            bool createdMaterial = EnsureUnlitMaterial();
            bool createdCharacterMaterial = EnsureCharacterMaterial();
            balance = AssetDatabase.LoadAssetAtPath<GameBalance>(Root + "/Core/GameBalance.asset");
            if (balance == null)
            {
                balance = ScriptableObject.CreateInstance<GameBalance>();
                AssetDatabase.CreateAsset(balance, Root + "/Core/GameBalance.asset");
            }
            EnsurePrefabs();
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(MainScene) == null) CreateMainScene();
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ArtScene) == null) CreateArtScene();
            if (createdMaterial || createdCharacterMaterial) UpgradeGeneratedVisuals();
            ConfigureBuildScenes();
            PlayerSettings.productName = "ChangbanDemo";
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorSceneManager.OpenScene(MainScene);
            Debug.Log("Changban setup complete. Existing scenes and prefabs were preserved.");
        }

        private static void EnsureFolders()
        {
            string[] folders = {
                Root + "/Scenes", Root + "/Scenes/Tests", Root + "/Features", Root + "/Features/Player",
                Root + "/Features/Combat", Root + "/Features/Enemies", Root + "/Features/Level",
                Root + "/Core", Root + "/Art", Root + "/Art/Generated", Root + "/Art/Characters",
                Root + "/Art/Characters/ZhaoYun", Root + "/Art/Characters/ZhaoYun/Prototype",
                Root + "/UI", Root + "/Editor", Root + "/Tests", Root + "/Prefabs"
            };
            foreach (string path in folders)
            {
                if (AssetDatabase.IsValidFolder(path)) continue;
                string parent = path.Substring(0, path.LastIndexOf('/'));
                string name = path.Substring(path.LastIndexOf('/') + 1);
                AssetDatabase.CreateFolder(parent, name);
            }
        }

        private static void EnsureImportedArt()
        {
            const string path = Root + "/Art/Characters/ZhaoYun/Prototype/ZY_Idle_SE_Prototype.png";
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) throw new FileNotFoundException("Prototype PNG is missing", path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 400;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.maxTextureSize = 2048;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            var spriteSettings = new TextureImporterSettings();
            importer.ReadTextureSettings(spriteSettings);
            spriteSettings.spriteMeshType = SpriteMeshType.FullRect;
            spriteSettings.spriteAlignment = (int)SpriteAlignment.Custom;
            spriteSettings.spritePivot = new Vector2(0.45f, 0.08f);
            importer.SetTextureSettings(spriteSettings);
            importer.SaveAndReimport();
        }

        private static Sprite EnsureGeneratedSprite(string name, bool circular)
        {
            string path = Root + "/Art/Generated/" + name + ".png";
            if (!File.Exists(path))
            {
                var texture = new Texture2D(32, 32, TextureFormat.RGBA32, false);
                var colors = new Color32[32 * 32];
                for (int y = 0; y < 32; y++)
                    for (int x = 0; x < 32; x++)
                    {
                        float radius = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(16, 16));
                        colors[y * 32 + x] = new Color32(255, 255, 255, (byte)(!circular || radius <= 15f ? 255 : 0));
                    }
                texture.SetPixels32(colors);
                texture.Apply();
                File.WriteAllBytes(path, texture.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(texture);
            }
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = 32;
            importer.filterMode = FilterMode.Bilinear;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static bool EnsureUnlitMaterial()
        {
            const string path = Root + "/Art/Generated/SpriteUnlit.mat";
            unlit = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (unlit != null) return false;
            Shader shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
            if (shader == null) throw new InvalidOperationException("URP 2D Unlit shader was not found");
            unlit = new Material(shader) { name = "Changban Sprite Unlit" };
            AssetDatabase.CreateAsset(unlit, path);
            return true;
        }

        private static bool EnsureCharacterMaterial()
        {
            const string path = Root + "/Art/Generated/CharacterUnlit.mat";
            characterUnlit = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (characterUnlit != null) return false;
            Shader shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
            if (shader == null) throw new InvalidOperationException("URP 2D Unlit shader was not found");
            characterUnlit = new Material(shader) { name = "Changban Character Unlit" };
            characterUnlit.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(Root + "/Art/Characters/ZhaoYun/Prototype/ZY_Idle_SE_Prototype.png");
            AssetDatabase.CreateAsset(characterUnlit, path);
            return true;
        }

        private static void UpgradeGeneratedVisuals()
        {
            string[] prefabs = { "Arrow", "Melee", "Archer", "Elite", "ZhaoYun" };
            foreach (string name in prefabs)
            {
                string path = Root + "/Prefabs/" + name + ".prefab";
                if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null) continue;
                GameObject root = PrefabUtility.LoadPrefabContents(path);
                if (UpgradeRenderers(root.GetComponentsInChildren<SpriteRenderer>(true)))
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                PrefabUtility.UnloadPrefabContents(root);
            }
            foreach (string path in new[] { MainScene, ArtScene })
            {
                if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) == null) continue;
                Scene scene = EditorSceneManager.OpenScene(path);
                var renderers = new List<SpriteRenderer>();
                foreach (GameObject root in scene.GetRootGameObjects())
                    renderers.AddRange(root.GetComponentsInChildren<SpriteRenderer>(true));
                if (UpgradeRenderers(renderers.ToArray())) EditorSceneManager.SaveScene(scene);
            }
        }

        private static bool UpgradeRenderers(SpriteRenderer[] renderers)
        {
            bool changed = false;
            foreach (SpriteRenderer renderer in renderers)
            {
                Material current = renderer.sharedMaterial;
                if (renderer.sprite != null && AssetDatabase.GetAssetPath(renderer.sprite) == Root + "/Art/Characters/ZhaoYun/Prototype/ZY_Idle_SE_Prototype.png")
                {
                    if (current != characterUnlit) { renderer.sharedMaterial = characterUnlit; changed = true; }
                    continue;
                }
                if (current != null && current.name != "Sprite-Lit-Default") continue;
                renderer.sharedMaterial = unlit;
                changed = true;
            }
            return changed;
        }

        private static void EnsurePrefabs()
        {
            string arrowPath = Root + "/Prefabs/Arrow.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(arrowPath) == null)
            {
                var obj = new GameObject("Arrow");
                AddSprite(obj, pixel, new Color(0.95f, 0.82f, 0.50f), new Vector2(0.45f, 0.09f));
                obj.AddComponent<ArrowProjectile>();
                SavePrefab(obj, arrowPath);
            }
            ArrowProjectile arrow = AssetDatabase.LoadAssetAtPath<GameObject>(arrowPath).GetComponent<ArrowProjectile>();
            EnsureEnemyPrefab("Melee", EnemyBrain.Role.Melee, new Color(0.63f, 0.25f, 0.22f), arrow);
            EnsureEnemyPrefab("Archer", EnemyBrain.Role.Archer, new Color(0.72f, 0.42f, 0.19f), arrow);
            EnsureEnemyPrefab("Elite", EnemyBrain.Role.Elite, new Color(0.36f, 0.17f, 0.20f), arrow);

            string playerPath = Root + "/Prefabs/ZhaoYun.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(playerPath) == null)
            {
                var obj = new GameObject("Zhao Yun");
                AddBody(obj, new Vector2(0.50f, 0.42f));
                obj.AddComponent<Health>();
                var motor = obj.AddComponent<PlayerMotor>(); motor.balance = balance;
                var attack = obj.AddComponent<PlayerAttack>(); attack.balance = balance;
                var art = new GameObject("Static SE Prototype");
                art.transform.SetParent(obj.transform, false);
                art.transform.localScale = Vector3.one * 0.66f;
                var artRenderer = art.AddComponent<SpriteRenderer>();
                artRenderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(Root + "/Art/Characters/ZhaoYun/Prototype/ZY_Idle_SE_Prototype.png");
                artRenderer.sharedMaterial = characterUnlit;
                art.AddComponent<WorldYSort>();
                var indicator = new GameObject("Direction Indicator");
                indicator.transform.SetParent(obj.transform, false);
                AddSprite(indicator, pixel, new Color(0.27f, 0.87f, 0.91f, 0.9f), new Vector2(0.12f, 0.38f));
                indicator.GetComponent<SpriteRenderer>().sortingOrder = 300;
                var trail = new GameObject("Spear Trail");
                trail.transform.SetParent(obj.transform, false);
                AddSprite(trail, pixel, new Color(0.78f, 0.93f, 1f, 0.72f), new Vector2(1f, 0.12f));
                trail.GetComponent<SpriteRenderer>().sortingOrder = 290;
                SavePrefab(obj, playerPath);
            }
        }

        private static void EnsureEnemyPrefab(string label, EnemyBrain.Role role, Color color, ArrowProjectile arrow)
        {
            string path = Root + "/Prefabs/" + label + ".prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null) return;
            var obj = new GameObject(label);
            AddSprite(obj, disc, color, role == EnemyBrain.Role.Elite ? new Vector2(1.15f, 1.15f) : new Vector2(0.75f, 0.75f));
            obj.AddComponent<WorldYSort>();
            AddBody(obj, role == EnemyBrain.Role.Elite ? new Vector2(0.7f, 0.65f) : new Vector2(0.55f, 0.5f));
            obj.AddComponent<Health>();
            var brain = obj.AddComponent<EnemyBrain>();
            brain.role = role; brain.balance = balance; brain.arrowPrefab = arrow;
            var warning = new GameObject("Warning");
            warning.transform.SetParent(obj.transform, false);
            AddSprite(warning, disc, new Color(1f, 0.10f, 0.05f, 0.28f), role == EnemyBrain.Role.Archer ? new Vector2(1f, 1f) : new Vector2(1.8f, 1.8f));
            warning.GetComponent<SpriteRenderer>().sortingOrder = -100;
            warning.GetComponent<SpriteRenderer>().enabled = false;
            SavePrefab(obj, path);
        }

        private static void AddBody(GameObject obj, Vector2 colliderSize)
        {
            var body = obj.AddComponent<Rigidbody2D>();
            body.gravityScale = 0;
            body.freezeRotation = true;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            var collider = obj.AddComponent<CapsuleCollider2D>();
            collider.size = colliderSize;
        }

        private static SpriteRenderer AddSprite(GameObject obj, Sprite sprite, Color color, Vector2 scale)
        {
            var graphic = obj.AddComponent<SpriteRenderer>();
            graphic.sprite = sprite;
            graphic.color = color;
            graphic.sharedMaterial = unlit;
            obj.transform.localScale = new Vector3(scale.x, scale.y, 1);
            return graphic;
        }

        private static void SavePrefab(GameObject obj, string path)
        {
            PrefabUtility.SaveAsPrefabAsset(obj, path);
            UnityEngine.Object.DestroyImmediate(obj);
        }

        private static GameObject PlacePrefab(string name, Vector2 position)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/Prefabs/" + name + ".prefab");
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.transform.position = position;
            return instance;
        }

        private static void CreateMainScene()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Camera camera = CreateCamera(new Color(0.21f, 0.23f, 0.20f), 5.2f);
            DrawRect("Village ground", new Vector2(-8, 0), new Vector2(15, 9), new Color(0.34f, 0.35f, 0.29f), -500);
            DrawRect("Ruined camp ground", new Vector2(6, 0), new Vector2(13, 9), new Color(0.38f, 0.34f, 0.28f), -500);
            DrawRect("Evacuation ground", new Vector2(18, 0), new Vector2(12, 9), new Color(0.32f, 0.36f, 0.30f), -500);
            DrawRect("Road", new Vector2(5, -0.4f), new Vector2(35, 3.0f), new Color(0.53f, 0.47f, 0.36f), -450);
            DrawRect("North wall", new Vector2(5, 4.4f), new Vector2(35, 0.35f), new Color(0.19f, 0.19f, 0.17f), -250);
            DrawRect("South wall", new Vector2(5, -4.4f), new Vector2(35, 0.35f), new Color(0.19f, 0.19f, 0.17f), 250);
            DrawRect("Ruined wall A", new Vector2(3.6f, 2.7f), new Vector2(3.2f, 0.45f), new Color(0.28f, 0.27f, 0.24f), 0);
            DrawRect("Ruined wall B", new Vector2(9, -2.9f), new Vector2(2.6f, 0.45f), new Color(0.28f, 0.27f, 0.24f), 100);

            var player = PlacePrefab("ZhaoYun", new Vector2(-10, -0.2f)).GetComponent<PlayerMotor>();
            var follow = camera.gameObject.AddComponent<CameraFollow>(); follow.target = player.transform;
            var rescue = DrawRect("A Dou — Press E", new Vector2(6, 0.5f), new Vector2(0.8f, 0.8f), new Color(0.97f, 0.8f, 0.25f), -50);
            var exit = DrawRect("Evacuation marker", new Vector2(19.2f, 0), new Vector2(1.4f, 2.2f), new Color(0.25f, 0.8f, 0.44f, 0.65f), -90);
            PlacePrefab("Melee", new Vector2(-5, 1.4f));
            PlacePrefab("Melee", new Vector2(-2.5f, -1.5f));
            PlacePrefab("Archer", new Vector2(0.5f, 2.2f));
            PlacePrefab("Melee", new Vector2(2.3f, -1.9f));
            PlacePrefab("Melee", new Vector2(5, 2.1f));
            PlacePrefab("Archer", new Vector2(8, -2f));

            var directorObj = new GameObject("Level Director");
            var director = directorObj.AddComponent<LevelDirector>();
            director.balance = balance;
            director.player = player;
            director.rescuePoint = rescue.transform;
            director.exitPoint = exit.transform;
            director.meleePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/Prefabs/Melee.prefab").GetComponent<EnemyBrain>();
            director.archerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/Prefabs/Archer.prefab").GetComponent<EnemyBrain>();
            director.elitePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/Prefabs/Elite.prefab").GetComponent<EnemyBrain>();
            var hud = new GameObject("HUD").AddComponent<GameHud>(); hud.level = director;
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), MainScene);
        }

        private static GameObject DrawRect(string name, Vector2 at, Vector2 size, Color color, int order)
        {
            var obj = new GameObject(name);
            var renderer = AddSprite(obj, pixel, color, size);
            renderer.sortingOrder = order;
            obj.transform.position = at;
            return obj;
        }

        private static Camera CreateCamera(Color background, float size)
        {
            var obj = new GameObject("Main Camera");
            obj.tag = "MainCamera";
            obj.transform.position = new Vector3(0, 0, -10);
            var camera = obj.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = size;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = background;
            return camera;
        }

        private static void CreateArtScene()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Camera camera = CreateCamera(new Color(0.72f, 0.72f, 0.71f), 5);
            var ground = DrawRect("Foot contact reference", new Vector2(0, -0.06f), new Vector2(5, 0.03f), new Color(0.12f, 0.12f, 0.12f), -100);
            var shadow = new GameObject("Separate ground shadow");
            AddSprite(shadow, disc, new Color(0, 0, 0, 0.25f), new Vector2(1.1f, 0.3f));
            shadow.transform.position = new Vector2(0, 0.02f);
            shadow.GetComponent<SpriteRenderer>().sortingOrder = -50;
            var character = new GameObject("Zhao Yun static prototype");
            var renderer = character.AddComponent<SpriteRenderer>();
            renderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(Root + "/Art/Characters/ZhaoYun/Prototype/ZY_Idle_SE_Prototype.png");
            renderer.sharedMaterial = characterUnlit;
            renderer.sortingOrder = 0;
            var controller = new GameObject("Art Validation Controls").AddComponent<ArtValidationControls>();
            controller.testCamera = camera;
            controller.character = renderer;
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), ArtScene);
        }

        private static void ConfigureBuildScenes()
        {
            var scenes = new List<EditorBuildSettingsScene>
            {
                new EditorBuildSettingsScene(MainScene, true),
                new EditorBuildSettingsScene(ArtScene, false)
            };
            foreach (var scene in EditorBuildSettings.scenes)
                if (scene.path != MainScene && scene.path != ArtScene)
                    scenes.Add(scene.path == "Assets/Scenes/SampleScene.unity" ?
                        new EditorBuildSettingsScene(scene.path, false) : scene);
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
