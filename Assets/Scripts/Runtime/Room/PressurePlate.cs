using UnityEngine;

namespace Game.Room
{
    /// <summary>
    /// Trigger area that counts the mass of bodies standing in it. A heavy plushie (the bear) holds a
    /// plate down; a light one (the rabbit) does not. With <c>m_Latches</c> it is a lever: the first
    /// press sticks until the room resets.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public sealed class PressurePlate : MonoBehaviour, IRoomResettable
    {
        [SerializeField] float m_RequiredMass = 3f;
        [SerializeField] bool m_Latches;
        [SerializeField] SpriteRenderer m_Visual;
        [SerializeField] Color m_IdleColor = new Color(0.55f, 0.35f, 0.35f);
        [SerializeField] Color m_ActiveColor = new Color(0.9f, 0.75f, 0.2f);

        PlateState<EntityId> m_State;

        public bool Active => m_State.Active;

        void Awake()
        {
            m_State = new PlateState<EntityId>(m_RequiredMass, m_Latches);
            Refresh();
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            var body = other.attachedRigidbody;
            if (body == null)
                return;
            m_State.Add(body.GetEntityId(), body.mass);
            Refresh();
        }

        void OnTriggerExit2D(Collider2D other)
        {
            var body = other.attachedRigidbody;
            if (body == null)
                return;
            m_State.Remove(body.GetEntityId());
            Refresh();
        }

        public void ResetState()
        {
            m_State.Reset();
            Refresh();
        }

        void Refresh()
        {
            if (m_Visual != null)
                m_Visual.color = m_State.Active ? m_ActiveColor : m_IdleColor;
        }
    }
}
