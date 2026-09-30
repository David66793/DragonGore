using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Changban.EditorTools
{
    [InitializeOnLoad]
    public static class GameUiSetup
    {
        static GameUiSetup()
        {
            LevelDirector.EditorExitRequested -= StopEditorPlay;
            LevelDirector.EditorExitRequested += StopEditorPlay;
        }
        private static void StopEditorPlay()
        {
            if (!Application.isBatchMode) EditorApplication.isPlaying = false;
        }

        [MenuItem("Changban/Apply Game UI")]
        public static void Apply()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.OpenScene("Assets/Game/Scenes/ChangbanDemo.unity");
            GameHud hud = Object.FindAnyObjectByType<GameHud>();
            if (hud == null) throw new System.InvalidOperationException("Main scene HUD is missing.");
            SkillHud bar = hud.GetComponent<SkillHud>();
            if (bar == null)
            {
                bar = hud.gameObject.AddComponent<SkillHud>();
                bar.Configure(hud.level);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
            Debug.Log("Game UI applied: one scene SkillHud, pause/restart/quit menu.");
        }

        public static void VerifyAndBuild()
        {
            Apply();
            Apply();
            if (Object.FindObjectsByType<SkillHud>().Length != 1)
                throw new System.InvalidOperationException("Duplicate SkillHud after repeated setup.");
            DemoVerification.ValidateAssets();
            DemoVerification.BuildWindows();
        }
    }
}
