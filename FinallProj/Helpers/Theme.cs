using Spectre.Console;

namespace FinallProj.Helpers
{
    internal static class Theme
    {
        public const string Title = "#FEEFB8";
        public const string Accent = "#A5BCD6";
        public const string Success = "#A9C28A";
        public const string Error = "#E8857B";
        public const string Warn = "#F2B66D";
        public const string Muted = "#A89B94";

        public static Color TitleColor { get; private set; } = new Color(0xFE, 0xEF, 0xB8);
        public static Color AccentColor { get; private set; } = new Color(0xA5, 0xBC, 0xD6);
        public static Color SuccessColor { get; private set; } = new Color(0xA9, 0xC2, 0x8A);
        public static Color ErrorColor { get; private set; } = new Color(0xE8, 0x85, 0x7B);
        public static Color WarnColor { get; private set; } = new Color(0xF2, 0xB6, 0x6D);
    }
}
