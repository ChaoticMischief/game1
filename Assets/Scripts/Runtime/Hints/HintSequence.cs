using System.Collections.Generic;

namespace Game.Hints
{
    /// <summary>
    /// Tiered hints for one puzzle, shown one tier per request: a nudge, then who-does-what, then the
    /// solution. Asking again after the last tier repeats it. Hint text is data supplied by the caller.
    /// </summary>
    public sealed class HintSequence
    {
        readonly IReadOnlyList<string> m_Tiers;

        public HintSequence(IReadOnlyList<string> tiers)
        {
            m_Tiers = tiers ?? new string[0];
        }

        public int Count => m_Tiers.Count;

        /// <summary>How many tiers have been revealed so far.</summary>
        public int Shown { get; private set; }

        public bool HasMore => Shown < Count;

        /// <summary>Reveals and returns the next tier, or the last one again once all are shown. Empty if there are none.</summary>
        public string Next()
        {
            if (Count == 0)
                return string.Empty;
            if (HasMore)
                Shown++;
            return m_Tiers[Shown - 1];
        }

        public void Reset() => Shown = 0;
    }
}
