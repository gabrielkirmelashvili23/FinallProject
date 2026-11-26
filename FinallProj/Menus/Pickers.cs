using FinallProj.Exceptions;
using FinallProj.Helpers;
using FinallProj.Models;
using FinallProj.Models.Enums;
using static FinallProj.Helpers.Translator;

namespace FinallProj.Menus
{
    internal static class Pickers
    {
        public static Group PickGroup(User user, string title = "Choose a group")
        {
            List<Group> groups = Access.GroupsFor(user);

            if (groups.Count == 0)
                throw new NotFoundException(T("You have no groups yet."));

            if (groups.Count == 1)
            {
                return groups[0];
            }

            List<string> labels = groups.Select(group => $"{group.Name}  ({App.CourseManager.GetAllCourses().First(course => course.Id == group.CourseId).Code})").ToList();
            int choice = Ui.ChooseIndex(title, labels, false, false, false);

            return groups[choice];
        }

        public static Student PickStudentByRoll(User user)
        {
            int roll = Ui.ReadPositiveNumber("Enter the student's roll number:");
            Student student = App.StudentManager.FindByRollNumber(roll);

            if (user.Role != UserRole.Admin && !Access.StudentsFor(user).Any(item => item.Id == student.Id))
                throw new ValidationException(T("This student is not in your groups!"));

            return student;
        }

        public static Student PickStudentOfGroup(Group group, string title = "Choose a student")
        {
            List<Student> students = App.StudentManager.GetAllStudents().Where(student => student.GroupId == group.Id).OrderBy(student => student.Name).ToList();

            if (students.Count == 0)
                throw new NotFoundException(T("This group has no students yet."));

            int choice = Ui.ChooseIndex(title, students.Select(student => $"{student.RollNumber}  {student.Name}").ToList());

            return students[choice];
        }

        public static string PickLecturer(User user, Group group)
        {
            if (user.Role == UserRole.Lecturer)
            {
                return user.Username;
            }

            Course? course = App.CourseManager.GetAllCourses().FirstOrDefault(item => item.Id == group.CourseId);

            if (course != null && course.LecturerUsername != string.Empty)
            {
                return course.LecturerUsername;
            }

            List<Lecturer> lecturers = App.UserManager.GetAllLecturers();

            if (lecturers.Count == 0)
                throw new NotFoundException(T("There are no lecturers yet. Add one first!"));

            int choice = Ui.ChooseIndex("Choose the lecturer", lecturers.Select(item => $"{item.Name}  ({item.Subject})").ToList());

            return lecturers[choice].Username;
        }

        public static int PickIndex<T2>(string title, List<T2> items, Func<T2, string> label)
        {
            if (items.Count == 0)
                throw new NotFoundException(T("Nothing to choose from yet."));

            return Ui.ChooseIndex(title, items.Select(label).ToList());
        }
    }
}
