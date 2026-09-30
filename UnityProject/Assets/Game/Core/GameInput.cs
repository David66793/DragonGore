using UnityEngine;
using UnityEngine.InputSystem;

namespace Changban
{
    // All controls are declared here, and Project Settings use the Input System only.
    public static class GameInput
    {
        public static Vector2 Move
        {
            get
            {
                var k = Keyboard.current;
                if (k == null) return Vector2.zero;
                float x = (k.dKey.isPressed ? 1 : 0) - (k.aKey.isPressed ? 1 : 0);
                float y = (k.wKey.isPressed ? 1 : 0) - (k.sKey.isPressed ? 1 : 0);
                return Vector2.ClampMagnitude(new Vector2(x, y), 1f);
            }
        }

        public static bool Attack => Keyboard.current != null && Keyboard.current.jKey.wasPressedThisFrame;
        public static bool Dodge => Keyboard.current != null && Keyboard.current.kKey.wasPressedThisFrame;
        public static bool Skill => Keyboard.current != null && Keyboard.current.lKey.wasPressedThisFrame;
        public static bool Interact => Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame;
        public static bool Pause => Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
        public static bool Restart => Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame;
        public static bool Start => Keyboard.current != null && (Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.spaceKey.wasPressedThisFrame);
    }
}
