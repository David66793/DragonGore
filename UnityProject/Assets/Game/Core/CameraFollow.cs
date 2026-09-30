using UnityEngine;

namespace Changban
{
    public sealed class CameraFollow : MonoBehaviour
    {
        public Transform target;
        public GameBalance balance;
        private Camera sceneCamera;

        private void Awake() { sceneCamera = GetComponent<Camera>(); }

        private void LateUpdate()
        {
            if (target == null || sceneCamera == null) return;
            GameBalance settings = balance != null ? balance : LevelDirector.Instance?.balance;
            if (settings == null) return;
            float halfHeight = sceneCamera.orthographicSize;
            float halfWidth = halfHeight * sceneCamera.aspect;
            Vector3 p = transform.position;
            p.x = ClampCameraAxis(target.position.x, settings.visibleGroundMin.x, settings.visibleGroundMax.x, halfWidth);
            p.y = ClampCameraAxis(target.position.y, settings.visibleGroundMin.y, settings.visibleGroundMax.y, halfHeight);
            transform.position = p;
        }

        private static float ClampCameraAxis(float targetPosition, float groundMin, float groundMax, float halfView)
        {
            float minimum = groundMin + halfView;
            float maximum = groundMax - halfView;
            return minimum <= maximum ? Mathf.Clamp(targetPosition, minimum, maximum) : (groundMin + groundMax) * 0.5f;
        }
    }
}
