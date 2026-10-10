using UnityEngine;

namespace Game.Player
{
    /// <summary>Jump physics helpers, kept free of scene state so EditMode tests can cover them.</summary>
    public static class JumpMath
    {
        /// <summary>Upward launch speed that reaches <paramref name="height"/> under <paramref name="gravity"/> (both positive).</summary>
        public static float VelocityForHeight(float height, float gravity)
        {
            if (height <= 0f || gravity <= 0f)
                return 0f;
            return Mathf.Sqrt(2f * gravity * height);
        }
    }
}
