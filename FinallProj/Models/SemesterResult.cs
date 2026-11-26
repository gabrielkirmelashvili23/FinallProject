using FinallProj.Helpers;
using FinallProj.Models.Enums;

namespace FinallProj.Models
{
    internal class ExamLine
    {
        public Exam Exam { get; set; } = new Exam();
        public int Attempts { get; set; }
        public int BestPoints { get; set; }
        public double Percent { get; set; }
        public bool Passed { get; set; }
    }

    internal class SemesterResult
    {
        public Semester Semester { get; set; } = new Semester(2000, 1);
        public int ActivityPoints { get; set; }
        public double ActivityPercent { get; set; }
        public double? ExamPercent { get; set; }
        public double TotalPercent { get; set; }
        public double Gpa { get; set; }
        public char Letter { get; set; }
        public FinalStatus FinalStatus { get; set; }
        public bool Closed { get; set; }
        public bool Provisional { get; set; }
        public List<ExamLine> Exams { get; set; } = new List<ExamLine>();
    }

    internal class YearResult
    {
        public int StartYear { get; set; }
        public List<SemesterResult> Semesters { get; set; } = new List<SemesterResult>();
        public double Gpa { get; set; }
        public char Letter { get; set; }
        public YearOutcome Outcome { get; set; }
        public bool IsClosed { get; set; }
    }

    internal class RankRow
    {
        public int Position { get; set; }
        public Student Student { get; set; } = new Student();
        public int Points { get; set; }
        public int Entries { get; set; }
    }
}
