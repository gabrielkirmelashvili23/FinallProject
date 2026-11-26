using FinallProj.Helpers;
using FinallProj.Models.Enums;

namespace FinallProj.Models
{
    internal class Submission
    {
        public int Id { get; set; }
        public int AssignmentId { get; set; }
        public int StudentId { get; set; }
        public DateTime Date { get; set; }
        public SubmissionKind Kind { get; set; }
        public string Content { get; set; } = string.Empty;
        public bool IsLate { get; set; }
        public bool IsReviewed { get; set; }
        public int Score { get; set; }
        public string Comment { get; set; } = string.Empty;
        public string ReviewedBy { get; set; } = string.Empty;
        public DateTime ReviewedDate { get; set; }

        public string ToFileLine()
        {
            return $"{Id}|{AssignmentId}|{StudentId}|{Format.Date(Date)}|{(int)Kind}|{DataStore.Clean(Content)}|{(IsLate ? 1 : 0)}|{(IsReviewed ? 1 : 0)}|{Score}|{DataStore.Clean(Comment)}|{ReviewedBy}|{Format.Date(ReviewedDate)}";
        }

        public static Submission FromFileLine(string line)
        {
            string[] p = line.Split('|');

            return new Submission
            {
                Id = int.Parse(p[0]),
                AssignmentId = int.Parse(p[1]),
                StudentId = int.Parse(p[2]),
                Date = Format.ParseDate(p[3]),
                Kind = (SubmissionKind)int.Parse(p[4]),
                Content = p[5],
                IsLate = p[6] == "1",
                IsReviewed = p[7] == "1",
                Score = int.Parse(p[8]),
                Comment = p[9],
                ReviewedBy = p[10],
                ReviewedDate = Format.ParseDate(p[11])
            };
        }
    }
}
