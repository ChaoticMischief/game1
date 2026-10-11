namespace Game.Player
{
    /// <summary>Which plushie is active. Wraps around when cycling; knows nothing about scenes or input.</summary>
    public sealed class PlushieSelection
    {
        public PlushieSelection(int count)
        {
            Count = count < 0 ? 0 : count;
        }

        public int Count { get; }

        public int Index { get; private set; }

        /// <summary>Activates the next plushie, wrapping to the first. Returns true if the active one changed.</summary>
        public bool Next() => Select(Count == 0 ? 0 : (Index + 1) % Count);

        /// <summary>Activates the previous plushie, wrapping to the last. Returns true if the active one changed.</summary>
        public bool Previous() => Select(Count == 0 ? 0 : (Index + Count - 1) % Count);

        /// <summary>Activates plushie <paramref name="index"/> if it exists. Returns true if the active one changed.</summary>
        public bool Select(int index)
        {
            if (index < 0 || index >= Count || index == Index)
                return false;
            Index = index;
            return true;
        }

        public void Reset() => Index = 0;
    }
}
