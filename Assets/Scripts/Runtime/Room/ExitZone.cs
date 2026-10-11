using System.Collections.Generic;
using Game.Player;
using UnityEngine;

namespace Game.Room
{
    /// <summary>Completes the room once every plushie in it is inside this trigger.</summary>
    [RequireComponent(typeof(Collider2D))]
    public sealed class ExitZone : MonoBehaviour
    {
        readonly HashSet<PlushieController> m_Inside = new HashSet<PlushieController>();
        RoomController m_Room;
        int m_Required;

        void Start()
        {
            m_Room = GetComponentInParent<RoomController>();
            m_Required = m_Room.GetComponentsInChildren<PlushieController>(true).Length;
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.TryGetComponent(out PlushieController plushie))
                return;
            m_Inside.Add(plushie);
            if (m_Inside.Count >= m_Required)
                m_Room.Complete();
        }

        void OnTriggerExit2D(Collider2D other)
        {
            if (other.TryGetComponent(out PlushieController plushie))
                m_Inside.Remove(plushie);
        }
    }
}
