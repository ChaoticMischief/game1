using Game.Room;
using UnityEngine;

namespace Game.Player
{
    /// <summary>
    /// One plushie's body: moves and jumps from the input it is handed. It never reads input itself;
    /// <see cref="PlushieRoster"/> (the "player slot") owns who controls it, so couch co-op can be
    /// added later. Abilities differ by tuning: the bear is heavy (presses plates) and jumps low, the
    /// rabbit is light and jumps high.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
    public sealed class PlushieController : MonoBehaviour, IRoomResettable
    {
        [SerializeField] string m_DisplayName = "Plushie";
        [SerializeField] float m_MoveSpeed = 4f;
        [SerializeField] float m_Acceleration = 30f;
        [SerializeField] float m_JumpHeight = 1f;
        [SerializeField] GameObject m_ActiveMarker;
        [SerializeField] string m_SelectAction = "";

        Rigidbody2D m_Body;
        BoxCollider2D m_Collider;
        readonly Collider2D[] m_GroundHits = new Collider2D[8];
        Vector3 m_StartPosition;
        bool m_JumpQueued;

        public string DisplayName => m_DisplayName;

        /// <summary>Name of the input action (e.g. "Player/SelectBear") that makes this the active plushie; empty for none.</summary>
        public string SelectAction => m_SelectAction;

        /// <summary>Horizontal input in [-1, 1], set by whoever controls this plushie.</summary>
        public float MoveInput { get; set; }

        public bool IsActive { get; private set; }

        void Awake()
        {
            m_Body = GetComponent<Rigidbody2D>();
            m_Collider = GetComponent<BoxCollider2D>();
            m_StartPosition = transform.position;
        }

        public void RequestJump() => m_JumpQueued = true;

        public void SetActive(bool active)
        {
            IsActive = active;
            if (m_ActiveMarker != null)
                m_ActiveMarker.SetActive(active);
        }

        public void ResetState()
        {
            transform.position = m_StartPosition;
            m_Body.linearVelocity = Vector2.zero;
            MoveInput = 0f;
            m_JumpQueued = false;
        }

        void FixedUpdate()
        {
            var velocity = m_Body.linearVelocity;
            velocity.x = Mathf.MoveTowards(velocity.x, MoveInput * m_MoveSpeed, m_Acceleration * Time.fixedDeltaTime);

            if (m_JumpQueued && IsGrounded())
            {
                var gravity = Physics2D.gravity.magnitude * m_Body.gravityScale;
                velocity.y = JumpMath.VelocityForHeight(m_JumpHeight, gravity);
            }
            m_JumpQueued = false;

            m_Body.linearVelocity = velocity;
        }

        bool IsGrounded()
        {
            var bounds = m_Collider.bounds;
            var feet = new Vector2(bounds.center.x, bounds.min.y - 0.05f);
            var size = new Vector2(bounds.size.x * 0.9f, 0.1f);
            var filter = new ContactFilter2D { useTriggers = false };
            var count = Physics2D.OverlapBox(feet, size, 0f, filter, m_GroundHits);
            for (var i = 0; i < count; i++)
            {
                if (m_GroundHits[i] != m_Collider)
                    return true;
            }
            return false;
        }
    }
}
