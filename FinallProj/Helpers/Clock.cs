namespace FinallProj.Helpers
{
    internal static class Clock
    {
        public static int DayOffset { get; set; } = 0;

        public static DateTime Today
        {
            get
            {
                return DateTime.Today.AddDays(DayOffset);
            }
        }
    }
}
