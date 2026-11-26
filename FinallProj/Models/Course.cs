using FinallProj.Helpers;

namespace FinallProj.Models
{
    internal class Course
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public decimal Tuition { get; set; }
        public int Installments { get; set; }
        public string LecturerUsername { get; set; } = string.Empty;

        public Course()
        {
        }

        public Course(int id, string code, string name, decimal tuition, int installments, string lecturerUsername)
        {
            Id = id;
            Code = code;
            Name = name;
            Tuition = tuition;
            Installments = installments;
            LecturerUsername = lecturerUsername;
        }

        public decimal GetMonthlyAmount()
        {
            return Math.Round(Tuition / Installments, 2);
        }

        public string ToFileLine()
        {
            return $"{Id}|{Code}|{Name}|{Format.Number(Tuition)}|{Installments}|{LecturerUsername}";
        }

        public override string ToString()
        {
            return $"{Code} - {Name}";
        }
    }
}
