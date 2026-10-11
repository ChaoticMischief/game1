using UnityEngine;

namespace Game.Room
{
    /// <summary>
    /// Keeps a whole room visible at any window shape. The room is a fixed size, so the camera widens
    /// its view when the window is narrower than the room and shows extra background when it is wider.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public sealed class CameraFit : MonoBehaviour
    {
        [SerializeField] float m_HalfWidth = 11f;
        [SerializeField] float m_HalfHeight = 6.3f;

        Camera m_Camera;
        float m_LastAspect;

        /// <summary>Orthographic size that fits a half-width by half-height area into a view of <paramref name="aspect"/> (width / height).</summary>
        public static float OrthographicSize(float halfWidth, float halfHeight, float aspect)
        {
            if (aspect <= 0f)
                return halfHeight;
            return Mathf.Max(halfHeight, halfWidth / aspect);
        }

        void Awake()
        {
            m_Camera = GetComponent<Camera>();
        }

        void LateUpdate()
        {
            var aspect = m_Camera.aspect;
            if (Mathf.Approximately(aspect, m_LastAspect))
                return;
            m_LastAspect = aspect;
            m_Camera.orthographicSize = OrthographicSize(m_HalfWidth, m_HalfHeight, aspect);
        }
    }
}
