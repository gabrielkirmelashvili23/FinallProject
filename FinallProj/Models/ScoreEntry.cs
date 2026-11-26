using FinallProj.Helpers;

namespace FinallProj.Models
{
    internal class ScoreEntry
    {
        public int Id { get; set; }
        public int StudentId { get; set; }
        public string LecturerUsername { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public int Points { get; set; }
        public string Reason { get; set; } = string.Empty;
        public int LectureId { get; set; }
        public int SubmissionId { get; set; }

        public string ToFileLine()
        {
            return $"{Id}|{StudentId}|{LecturerUsername}|{Format.Date(Date)}|{Points}|{DataStore.Clean(Reason)}|{LectureId}|{SubmissionId}";
        }

        public static ScoreEntry FromFileLine(string line)
        {
            string[] p = line.Split('|');

            return new ScoreEntry
            {
                Id = int.Parse(p[0]),
                StudentId = int.Parse(p[1]),
                LecturerUsername = p[2],
                Date = Format.ParseDate(p[3]),
                Points = int.Parse(p[4]),
                Reason = p[5],
                LectureId = int.Parse(p[6]),
                SubmissionId = int.Parse(p[7])
            };
        }
    }
}
