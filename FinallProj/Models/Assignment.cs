using FinallProj.Helpers;

namespace FinallProj.Models
{
    internal class Assignment
    {
        public int Id { get; set; }
        public int GroupId { get; set; }
        public string LecturerUsername { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime Created { get; set; }
        public DateTime Deadline { get; set; }
        public int MaxPoints { get; set; } = 10;

        public string ToFileLine()
        {
            return $"{Id}|{GroupId}|{LecturerUsername}|{DataStore.Clean(Title)}|{DataStore.Clean(Description)}|{Format.Date(Created)}|{Format.Date(Deadline)}|{MaxPoints}";
        }

        public static Assignment FromFileLine(string line)
        {
            string[] p = line.Split('|');

            return new Assignment
            {
                Id = int.Parse(p[0]),
                GroupId = int.Parse(p[1]),
                LecturerUsername = p[2],
                Title = p[3],
                Description = p[4],
                Created = Format.ParseDate(p[5]),
                Deadline = Format.ParseDate(p[6]),
                MaxPoints = int.Parse(p[7])
            };
        }
    }
}
