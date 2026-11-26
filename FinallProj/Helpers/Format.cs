using System.Globalization;

namespace FinallProj.Helpers
{
    internal static class Format
    {
        public static string Date(DateTime date)
        {
            return date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        }

        public static DateTime ParseDate(string text)
        {
            return DateTime.ParseExact(text, "yyyy-MM-dd", CultureInfo.InvariantCulture);
        }

        public static bool TryParseDate(string text, out DateTime date)
        {
            return DateTime.TryParseExact(text, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out date);
        }

        public static string Number(decimal number)
        {
            return number.ToString(CultureInfo.InvariantCulture);
        }

        public static decimal ParseNumber(string text)
        {
            return decimal.Parse(text, CultureInfo.InvariantCulture);
        }

        public static string Money(decimal amount)
        {
            return amount.ToString("N2", CultureInfo.InvariantCulture) + " GEL";
        }
    }
}
