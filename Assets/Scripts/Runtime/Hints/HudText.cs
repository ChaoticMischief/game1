namespace Game.Hints
{
    /// <summary>The words shown on the HUD, kept out of the MonoBehaviour so tests can cover them.</summary>
    public static class HudText
    {
        public const string ControlsSummary = "A / D move   Space jump   1 / 2 switch plushie   E hint   ? all controls";

        public const string HelpTitle = "Controls";

        public const string HelpBody =
            "Keyboard\n" +
            "  A / D or the arrow keys: move\n" +
            "  Space: jump\n" +
            "  1 / 2: switch plushie\n" +
            "  E: hint (or click the Man in the Moon)\n" +
            "  ? or F1: show / hide this help\n" +
            "\n" +
            "Gamepad\n" +
            "  Left stick: move\n" +
            "  A (south button): jump\n" +
            "  D-pad left / right: switch plushie\n" +
            "  Y (north button): hint\n" +
            "  Back / View: show / hide this help\n" +
            "\n" +
            "Tip: one plushie can hold a plate down while the other goes on.";

        public static string SelectedLine(string plushieName)
        {
            return "Currently selected plushie: " + (string.IsNullOrEmpty(plushieName) ? "none" : plushieName);
        }
    }
}
