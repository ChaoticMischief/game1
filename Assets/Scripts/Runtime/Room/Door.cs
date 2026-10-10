using UnityEngine;

namespace Game.Room
{
    /// <summary>A door that is open while any of its plates is active (held down, or latched like a lever).</summary>
    [RequireComponent(typeof(Collider2D))]
    public sealed class Door : MonoBehaviour, IRoomResettable
    {
        [SerializeField] PressurePlate[] m_Plates = new PressurePlate[0];
        [SerializeField] float m_OpenHeight = 4f;
        [SerializeField] float m_Speed = 6f;

        Collider2D m_Collider;
        Vector3 m_ClosedPosition;

        public bool IsOpen { get; private set; }

        void Awake()
        {
            m_Collider = GetComponent<Collider2D>();
            m_ClosedPosition = transform.position;
        }

        void Update()
        {
            IsOpen = false;
            foreach (var plate in m_Plates)
            {
                if (plate.Active)
                {
                    IsOpen = true;
                    break;
                }
            }

            var target = m_ClosedPosition + (IsOpen ? Vector3.up * m_OpenHeight : Vector3.zero);
            transform.position = Vector3.MoveTowards(transform.position, target, m_Speed * Time.deltaTime);
            m_Collider.enabled = !IsOpen;
        }

        public void ResetState()
        {
            transform.position = m_ClosedPosition;
            IsOpen = false;
            m_Collider.enabled = true;
        }
    }
}
