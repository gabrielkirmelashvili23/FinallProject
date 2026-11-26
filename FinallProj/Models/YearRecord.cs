using FinallProj.Helpers;
using FinallProj.Models.Enums;

namespace FinallProj.Models
{
    internal class YearRecord
    {
        public int StartYear { get; set; }
        public int StudentId { get; set; }
        public YearOutcome Outcome { get; set; }
        public double Gpa { get; set; }
        public char Letter { get; set; }
        public DateTime ClosedOn { get; set; }

        public string ToFileLine()
        {
            return $"{StartYear}|{StudentId}|{(int)Outcome}|{Format.Number((decimal)Gpa)}|{Letter}|{Format.Date(ClosedOn)}";
        }

        public static YearRecord FromFileLine(string line)
        {
            string[] p = line.Split('|');

            return new YearRecord
            {
                StartYear = int.Parse(p[0]),
                StudentId = int.Parse(p[1]),
                Outcome = (YearOutcome)int.Parse(p[2]),
                Gpa = (double)Format.ParseNumber(p[3]),
                Letter = p[4][0],
                ClosedOn = Format.ParseDate(p[5])
            };
        }
    }
}
