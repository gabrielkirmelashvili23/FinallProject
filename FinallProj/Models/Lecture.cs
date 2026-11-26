using FinallProj.Helpers;
using FinallProj.Models.Enums;

namespace FinallProj.Models
{
    internal class Lecture
    {
        public int Id { get; set; }
        public int GroupId { get; set; }
        public string LecturerUsername { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public string StartTime { get; set; } = "09:00";
        public int Minutes { get; set; } = 90;
        public string Title { get; set; } = string.Empty;
        public LectureFormat Format { get; set; } = LectureFormat.Room;
        public string Place { get; set; } = string.Empty;
        public LectureStatus Status { get; set; } = LectureStatus.Planned;

        public Lecture()
        {
        }

        public TimeSpan Start
        {
            get
            {
                TimeText.TryParse(StartTime, out TimeSpan start);
                return start;
            }
        }

        public TimeSpan End
        {
            get
            {
                return Start.Add(TimeSpan.FromMinutes(Minutes));
            }
        }

        public string EndTime
        {
            get
            {
                return TimeText.Format(End);
            }
        }

        public bool Overlaps(Lecture other)
        {
            return Date.Date == other.Date.Date && Start < other.End && other.Start < End;
        }

        public string ToFileLine()
        {
            return $"{Id}|{GroupId}|{LecturerUsername}|{Helpers.Format.Date(Date)}|{StartTime}|{Minutes}|{DataStore.Clean(Title)}|{(int)Format}|{DataStore.Clean(Place)}|{(int)Status}";
        }

        public static Lecture FromFileLine(string line)
        {
            string[] p = line.Split('|');

            return new Lecture
            {
                Id = int.Parse(p[0]),
                GroupId = int.Parse(p[1]),
                LecturerUsername = p[2],
                Date = Helpers.Format.ParseDate(p[3]),
                StartTime = p[4],
                Minutes = int.Parse(p[5]),
                Title = p[6],
                Format = (LectureFormat)int.Parse(p[7]),
                Place = p[8],
                Status = (LectureStatus)int.Parse(p[9])
            };
        }
    }
}
