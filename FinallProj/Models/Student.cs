using FinallProj.Exceptions;
using FinallProj.Models.Enums;
using static FinallProj.Helpers.Translator;

namespace FinallProj.Models
{
    internal class Student : User
    {
        public static char[] ValidGrades { get; private set; } = new char[] { 'A', 'B', 'C', 'D', 'E', 'F' };

        public int RollNumber { get; private set; }

        public char Grade { get; private set; } = 'N';

        public int GroupId { get; set; }

        public int ScholarshipPercent { get; private set; }

        public Student() : base(UserRole.Student)
        {
        }

        public Student(int id, string name, string username, int rollNumber)
            : base(id, name, username, UserRole.Student)
        {
            RollNumber = rollNumber;
        }

        public Student(int id, string name, string username, int rollNumber, char grade, int groupId, int scholarshipPercent)
            : base(id, name, username, UserRole.Student)
        {
            RollNumber = rollNumber;

            if (grade != 'N')
            {
                UpdateGrade(grade);
            }

            GroupId = groupId;
            SetScholarship(scholarshipPercent);
        }

        public static bool IsValidGrade(char grade)
        {
            foreach (char validGrade in ValidGrades)
            {
                if (validGrade == grade)
                {
                    return true;
                }
            }

            return false;
        }

        public void UpdateGrade(char newGrade)
        {
            if (!IsValidGrade(newGrade))
                throw new InvalidGradeException(T("Grade must be one of: {0}!", string.Join(", ", ValidGrades)));

            Grade = newGrade;
        }

        public void SetScholarship(int percent)
        {
            if (percent < 0 || percent > 100)
                throw new ValidationException(T("Scholarship must be between 0 and 100 percent!"));

            ScholarshipPercent = percent;
        }

        public bool IsGraded()
        {
            return Grade != 'N';
        }

        public bool HasPassed()
        {
            return IsGraded() && Grade != 'F';
        }

        public int GetGradePoints()
        {
            int points = Grade switch
            {
                'A' => 5,
                'B' => 4,
                'C' => 3,
                'D' => 2,
                'E' => 1,
                _ => 0
            };

            return points;
        }

        public override string GetInfo()
        {
            string grade = IsGraded() ? Grade.ToString() : "-";

            return $"[N{RollNumber}] {Name} | {Username} | {T("Grade")}: {grade}";
        }

        public override string ToFileLine()
        {
            return base.ToFileLine() + $"|{RollNumber}|{Grade}|{GroupId}|{ScholarshipPercent}";
        }
    }
}
