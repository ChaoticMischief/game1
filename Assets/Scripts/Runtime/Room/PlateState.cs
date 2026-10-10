using System.Collections.Generic;

namespace Game.Room
{
    /// <summary>
    /// Pressure plate logic, keyed by whatever identifies a body (an <c>EntityId</c> in the scene): bodies standing on it add their mass, and it counts as pressed once the
    /// total reaches <see cref="RequiredMass"/>. A latching plate (a lever) stays <see cref="Latched"/>
    /// after its first press until reset.
    /// </summary>
    public sealed class PlateState<TKey>
    {
        readonly Dictionary<TKey, float> m_Masses = new Dictionary<TKey, float>();

        public PlateState(float requiredMass, bool latches)
        {
            RequiredMass = requiredMass;
            Latches = latches;
        }

        public float RequiredMass { get; }

        public bool Latches { get; }

        public float TotalMass { get; private set; }

        public bool Pressed => TotalMass >= RequiredMass && m_Masses.Count > 0;

        public bool Latched { get; private set; }

        /// <summary>True while the plate is holding whatever it controls open.</summary>
        public bool Active => Pressed || Latched;

        public void Add(TKey id, float mass)
        {
            m_Masses[id] = mass;
            Recalculate();
        }

        public void Remove(TKey id)
        {
            if (m_Masses.Remove(id))
                Recalculate();
        }

        public void Reset()
        {
            m_Masses.Clear();
            TotalMass = 0f;
            Latched = false;
        }

        void Recalculate()
        {
            var total = 0f;
            foreach (var mass in m_Masses.Values)
                total += mass;
            TotalMass = total;
            if (Latches && Pressed)
                Latched = true;
        }
    }
}
