using FinallProj.Helpers;

namespace FinallProj.Models
{
    internal class Exam
    {
        public int Id { get; set; }
        public int GroupId { get; set; }
        public string Title { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public string StartTime { get; set; } = "10:00";
        public string Place { get; set; } = string.Empty;
        public string Details { get; set; } = string.Empty;
        public bool IsFinal { get; set; }
        public int MaxPoints { get; set; } = 100;
        public int RetakeOf { get; set; }
        public string CreatedBy { get; set; } = string.Empty;

        public int RootId
        {
            get
            {
                return RetakeOf == 0 ? Id : RetakeOf;
            }
        }

        public string ToFileLine()
        {
            return $"{Id}|{GroupId}|{DataStore.Clean(Title)}|{Format.Date(Date)}|{StartTime}|{DataStore.Clean(Place)}|{DataStore.Clean(Details)}|{(IsFinal ? 1 : 0)}|{MaxPoints}|{RetakeOf}|{CreatedBy}";
        }

        public static Exam FromFileLine(string line)
        {
            string[] p = line.Split('|');

            return new Exam
            {
                Id = int.Parse(p[0]),
                GroupId = int.Parse(p[1]),
                Title = p[2],
                Date = Format.ParseDate(p[3]),
                StartTime = p[4],
                Place = p[5],
                Details = p[6],
                IsFinal = p[7] == "1",
                MaxPoints = int.Parse(p[8]),
                RetakeOf = int.Parse(p[9]),
                CreatedBy = p[10]
            };
        }
    }
}
