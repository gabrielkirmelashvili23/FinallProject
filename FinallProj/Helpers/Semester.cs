using static FinallProj.Helpers.Translator;

namespace FinallProj.Helpers
{
    internal class Semester
    {
        public int StartYear { get; private set; }
        public int Number { get; private set; }

        public Semester(int startYear, int number)
        {
            StartYear = startYear;
            Number = number;
        }

        public static Semester Of(DateTime date)
        {
            if (date.Month >= 9)
            {
                return new Semester(date.Year, 1);
            }

            if (date.Month == 1)
            {
                return new Semester(date.Year - 1, 1);
            }

            return new Semester(date.Year - 1, 2);
        }

        public DateTime Start
        {
            get
            {
                return Number == 1 ? new DateTime(StartYear, 9, 1) : new DateTime(StartYear + 1, 2, 1);
            }
        }

        public DateTime End
        {
            get
            {
                return Number == 1 ? new DateTime(StartYear + 1, 1, 31) : new DateTime(StartYear + 1, 8, 31);
            }
        }

        public string YearLabel
        {
            get
            {
                return $"{StartYear}/{StartYear + 1}";
            }
        }

        public string Label
        {
            get
            {
                return T("{0}, semester {1}", YearLabel, Number);
            }
        }

        public string ShortLabel
        {
            get
            {
                return $"{YearLabel} / {Number}";
            }
        }

        public string Key
        {
            get
            {
                return $"{StartYear}-{Number}";
            }
        }

        public bool Contains(DateTime date)
        {
            return date.Date >= Start && date.Date <= End;
        }

        public Semester Next()
        {
            return Number == 1 ? new Semester(StartYear, 2) : new Semester(StartYear + 1, 1);
        }

        public bool IsBeforeOrEqual(Semester other)
        {
            return StartYear < other.StartYear || (StartYear == other.StartYear && Number <= other.Number);
        }

        public static List<Semester> Range(DateTime from, DateTime to)
        {
            List<Semester> list = new List<Semester>();
            Semester current = Of(from);
            Semester last = Of(to);

            while (current.IsBeforeOrEqual(last))
            {
                list.Add(current);
                current = current.Next();
            }

            return list;
        }
    }
}
