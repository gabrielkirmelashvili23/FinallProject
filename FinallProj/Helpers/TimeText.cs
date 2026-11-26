using System.Text.RegularExpressions;

namespace FinallProj.Helpers
{
    internal static class TimeText
    {
        private static Regex _pattern = new Regex(@"^([01]?\d|2[0-3]):([0-5]\d)$");

        public static bool TryParse(string text, out TimeSpan time)
        {
            time = TimeSpan.Zero;

            Match match = _pattern.Match(text.Trim());

            if (!match.Success)
            {
                return false;
            }

            time = new TimeSpan(int.Parse(match.Groups[1].Value), int.Parse(match.Groups[2].Value), 0);

            return true;
        }

        public static string Normalize(string text)
        {
            TryParse(text, out TimeSpan time);

            return Format(time);
        }

        public static string Format(TimeSpan time)
        {
            int hours = (int)time.TotalHours % 24;

            return $"{hours:D2}:{time.Minutes:D2}";
        }
    }
}
