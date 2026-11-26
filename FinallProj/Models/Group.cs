using FinallProj.Helpers;

namespace FinallProj.Models
{
    internal class Group
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int CourseId { get; set; }
        public int MaxStudents { get; set; }
        public DateTime StartDate { get; set; }

        public Group()
        {
        }

        public Group(int id, string name, int courseId, int maxStudents, DateTime startDate)
        {
            Id = id;
            Name = name;
            CourseId = courseId;
            MaxStudents = maxStudents;
            StartDate = startDate;
        }

        public string ToFileLine()
        {
            return $"{Id}|{Name}|{CourseId}|{MaxStudents}|{Format.Date(StartDate)}";
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
