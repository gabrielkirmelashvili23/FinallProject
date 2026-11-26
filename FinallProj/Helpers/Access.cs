using FinallProj.Models;
using FinallProj.Models.Enums;

namespace FinallProj.Helpers
{
    internal static class Access
    {
        public static List<Group> GroupsFor(User user)
        {
            if (user.Role == UserRole.Admin)
            {
                return App.CourseManager.GetAllGroups();
            }

            if (user.Role == UserRole.Lecturer)
            {
                return App.CourseManager.GetAllGroups()
                    .Where(group => TeachesGroup(user.Username, group))
                    .ToList();
            }

            if (user is Student student && student.GroupId != 0)
            {
                Group? own = App.CourseManager.GetGroupOfStudent(student);

                return own == null ? new List<Group>() : new List<Group> { own };
            }

            return new List<Group>();
        }

        public static bool TeachesGroup(string username, Group group)
        {
            Course? course = App.CourseManager.GetAllCourses().FirstOrDefault(item => item.Id == group.CourseId);

            return course != null && course.LecturerUsername == username;
        }

        public static bool CanManageGroup(User user, int groupId)
        {
            if (user.Role == UserRole.Admin)
            {
                return true;
            }

            return user.Role == UserRole.Lecturer && GroupsFor(user).Any(group => group.Id == groupId);
        }

        public static List<Student> StudentsFor(User user)
        {
            List<int> groupIds = GroupsFor(user).Select(group => group.Id).ToList();

            return App.StudentManager.GetAllStudents()
                .Where(student => student.GroupId != 0 && groupIds.Contains(student.GroupId))
                .OrderBy(student => student.Name)
                .ToList();
        }

        public static string GroupName(int groupId)
        {
            Group? group = App.CourseManager.GetAllGroups().FirstOrDefault(item => item.Id == groupId);

            return group == null ? "-" : group.Name;
        }

        public static string LecturerName(string username)
        {
            User? user = App.UserManager.GetAllUsers().FirstOrDefault(item => item.Username == username);

            return user == null ? "-" : user.Name;
        }
    }
}
