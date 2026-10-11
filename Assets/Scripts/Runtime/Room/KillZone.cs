using Game.Player;
using UnityEngine;

namespace Game.Room
{
    /// <summary>Resets the room when a plushie leaves the playable area (falls out of the world).</summary>
    [RequireComponent(typeof(Collider2D))]
    public sealed class KillZone : MonoBehaviour
    {
        void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.TryGetComponent(out PlushieController _))
                return;
            var room = GetComponentInParent<RoomController>();
            if (room != null)
                room.ResetRoom();
        }
    }
}
