using FinallProj.Exceptions;
using FinallProj.Helpers;
using FinallProj.Models;
using static FinallProj.Helpers.Translator;

namespace FinallProj.Managers
{
    internal class StudentManager
    {
        private List<User> _users;

        public StudentManager()
        {
            _users = new List<User>();
        }

        public StudentManager(List<User> users)
        {
            _users = users;
        }

        public int GetNextRollNumber()
        {
            List<Student> students = GetAllStudents();

            if (students.Count == 0)
            {
                return 101;
            }

            return students.Max(student => student.RollNumber) + 1;
        }

        public Student AddStudent(string name, string username, string password, string question, string answer)
        {
            int rollNumber = GetNextRollNumber();

            if (username == string.Empty)
            {
                username = $"s{rollNumber}";
            }

            if (!UserManager.IsValidUsername(username))
                throw new ValidationException(T("Username must have 3-20 characters: letters, digits, '.' or '_'!"));

            bool usernameTaken = _users.Any(user => string.Equals(user.Username, username, StringComparison.OrdinalIgnoreCase));

            if (usernameTaken)
                throw new DuplicateUsernameException(T("Username {0} is already taken!", username));

            int newId = 1;

            if (_users.Count > 0)
            {
                newId = _users.Max(user => user.Id) + 1;
            }

            Student newStudent = new Student(newId, name, username, rollNumber);
            newStudent.SetPassword(password);

            if (question != string.Empty)
            {
                newStudent.SetSecurity(question, answer);
            }

            _users.Add(newStudent);

            return newStudent;
        }

        public bool RollNumberExists(int rollNumber)
        {
            return _users
                .OfType<Student>()
                .Any(student => student.RollNumber == rollNumber);
        }

        public List<Student> GetAllStudents()
        {
            return _users
                .OfType<Student>()
                .OrderBy(student => student.RollNumber)
                .ToList();
        }

        public Student FindByRollNumber(int rollNumber)
        {
            foreach (Student student in GetAllStudents())
            {
                if (student.RollNumber == rollNumber)
                {
                    return student;
                }
            }

            throw new NotFoundException(T("Student with roll number {0} not found!", rollNumber));
        }

        public List<Student> SearchByName(string text)
        {
            return GetAllStudents()
                .Where(student => student.Name.Contains(text, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        public Student UpdateGrade(int rollNumber, char newGrade)
        {
            Student student = FindByRollNumber(rollNumber);
            student.UpdateGrade(newGrade);

            return student;
        }

        public List<Student> GetGradedStudents()
        {
            return GetAllStudents().FindAll(student => student.IsGraded());
        }

        public double GetAverageGradePoints()
        {
            List<Student> students = GetGradedStudents();

            if (students.Count == 0)
            {
                return 0;
            }

            return students.Average(student => student.GetGradePoints());
        }

        public List<IGrouping<char, Student>> GetStudentsGroupedByGrade()
        {
            return GetGradedStudents()
                .GroupBy(student => student.Grade)
                .OrderBy(group => group.Key)
                .ToList();
        }

        public List<Student> GetTopStudents(int count)
        {
            return GetGradedStudents()
                .OrderByDescending(student => student.GetGradePoints())
                .ThenBy(student => student.Name)
                .Take(count)
                .ToList();
        }

        public List<Student> GetPassedStudents()
        {
            Predicate<Student> passed = student => student.HasPassed();

            return GetAllStudents().FindAll(passed);
        }

        public List<Student> GetFailedStudents()
        {
            return GetGradedStudents().FindAll(student => !student.HasPassed());
        }

        public Student DeleteStudent(int rollNumber)
        {
            Student student = FindByRollNumber(rollNumber);
            _users.Remove(student);

            return student;
        }
    }
}
