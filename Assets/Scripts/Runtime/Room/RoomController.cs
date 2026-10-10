using System;
using UnityEngine;

namespace Game.Room
{
    /// <summary>
    /// Owns a room's lifecycle. <see cref="ResetRoom"/> is the single path that puts everything back
    /// to its start: falling out of the world uses it now, and the Horror-mode "caught" animation
    /// will end in it. Put every resettable thing under this object.
    /// </summary>
    public sealed class RoomController : MonoBehaviour
    {
        IRoomResettable[] m_Resettables;

        public event Action Completed;

        public bool IsComplete { get; private set; }

        void Awake()
        {
            m_Resettables = GetComponentsInChildren<IRoomResettable>(true);
        }

        public void Complete()
        {
            if (IsComplete)
                return;
            IsComplete = true;
            Completed?.Invoke();
        }

        public void ResetRoom()
        {
            IsComplete = false;
            foreach (var resettable in m_Resettables)
                resettable.ResetState();
        }
    }
}
