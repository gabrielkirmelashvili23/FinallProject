using FinallProj.Helpers;

namespace FinallProj.Models
{
    internal class ExamResult
    {
        public int Id { get; set; }
        public int ExamId { get; set; }
        public int StudentId { get; set; }
        public int Attempt { get; set; }
        public int Points { get; set; }
        public DateTime Date { get; set; }
        public string RecordedBy { get; set; } = string.Empty;

        public string ToFileLine()
        {
            return $"{Id}|{ExamId}|{StudentId}|{Attempt}|{Points}|{Format.Date(Date)}|{RecordedBy}";
        }

        public static ExamResult FromFileLine(string line)
        {
            string[] p = line.Split('|');

            return new ExamResult
            {
                Id = int.Parse(p[0]),
                ExamId = int.Parse(p[1]),
                StudentId = int.Parse(p[2]),
                Attempt = int.Parse(p[3]),
                Points = int.Parse(p[4]),
                Date = Format.ParseDate(p[5]),
                RecordedBy = p[6]
            };
        }
    }
}
