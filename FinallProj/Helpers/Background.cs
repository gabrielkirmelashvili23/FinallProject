namespace FinallProj.Helpers
{
    internal static class Background
    {
        public static string[] Keys { get; private set; } = { "chocolate", "java", "sceptre", "olive", "night", "default" };

        private static string[] _colors = { "432F2E", "231815", "4D0E12", "363321", "1E2B3C", "0C0C0C" };

        private const string TextColor = "FAF6F0";

        public static string Current { get; private set; } = "chocolate";

        public static void Set(string key)
        {
            if (Array.IndexOf(Keys, key) < 0)
            {
                return;
            }

            Current = key;
            Apply();
        }

        public static void Apply()
        {
            if (Current == "default")
            {
                Reset();
                return;
            }

            string color = _colors[Array.IndexOf(Keys, Current)];

            Console.Write($"\u001b]11;{Rgb(color)}\u001b\\\u001b]10;{Rgb(TextColor)}\u001b\\");
        }

        public static void Reset()
        {
            Console.Write("\u001b]111\u001b\\\u001b]110\u001b\\");
        }

        public static List<string> Swatches()
        {
            List<string> swatches = new List<string>();

            for (int i = 0; i < Keys.Length; i++)
            {
                string bg = "#" + _colors[i];
                string text = "#" + TextColor;

                swatches.Add($"[{text} on {bg}]  Aa  [/][{Theme.Title} on {bg}] ■ [/][{Theme.Accent} on {bg}] ■ [/][{Theme.Success} on {bg}] ■ [/][{Theme.Error} on {bg}] ■ [/][{text} on {bg}]  [/]");
            }

            return swatches;
        }

        private static string Rgb(string hex)
        {
            string r = hex.Substring(0, 2);
            string g = hex.Substring(2, 2);
            string b = hex.Substring(4, 2);

            return $"rgb:{r}/{g}/{b}";
        }
    }
}
