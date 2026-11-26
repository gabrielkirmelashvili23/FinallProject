using FinallProj.Exceptions;
using FinallProj.Models;
using static FinallProj.Helpers.Translator;

namespace FinallProj.Managers
{
    internal class CourseManager
    {
        private List<Course> _courses;
        private List<Group> _groups;

        public CourseManager(List<Course> courses, List<Group> groups)
        {
            _courses = courses;
            _groups = groups;
        }

        public List<Course> GetAllCourses()
        {
            return _courses.OrderBy(course => course.Code).ToList();
        }

        public List<Group> GetAllGroups()
        {
            return _groups.OrderBy(group => group.CourseId).ThenBy(group => group.Name).ToList();
        }

        public Course AddCourse(string code, string name, decimal tuition, int installments, string lecturerUsername)
        {
            if (_courses.Any(course => string.Equals(course.Code, code, StringComparison.OrdinalIgnoreCase)))
                throw new DuplicateCourseException(T("Course with code {0} already exists!", code));

            if (tuition <= 0)
                throw new ValidationException(T("Tuition must be greater than zero!"));

            if (installments < 1 || installments > 12)
                throw new ValidationException(T("Installments must be between 1 and 12!"));

            int newId = 1;

            if (_courses.Count > 0)
            {
                newId = _courses.Max(course => course.Id) + 1;
            }

            Course course = new Course(newId, code.ToUpper(), name, tuition, installments, lecturerUsername);
            _courses.Add(course);

            return course;
        }

        public Course FindCourse(int courseId)
        {
            Course? course = _courses.FirstOrDefault(item => item.Id == courseId);

            if (course == null)
                throw new NotFoundException(T("Course not found!"));

            return course;
        }

        public Course DeleteCourse(int courseId)
        {
            Course course = FindCourse(courseId);

            if (_groups.Any(group => group.CourseId == courseId))
                throw new ValidationException(T("Delete the groups of this course first!"));

            _courses.Remove(course);

            return course;
        }

        public Group AddGroup(string name, int courseId, int maxStudents, DateTime startDate)
        {
            FindCourse(courseId);

            if (_groups.Any(group => string.Equals(group.Name, name, StringComparison.OrdinalIgnoreCase)))
                throw new ValidationException(T("A group with this name already exists!"));

            if (maxStudents < 1 || maxStudents > 100)
                throw new ValidationException(T("Group size must be between 1 and 100!"));

            int newId = 1;

            if (_groups.Count > 0)
            {
                newId = _groups.Max(group => group.Id) + 1;
            }

            Group group = new Group(newId, name, courseId, maxStudents, startDate);
            _groups.Add(group);

            return group;
        }

        public Group FindGroup(int groupId)
        {
            Group? group = _groups.FirstOrDefault(item => item.Id == groupId);

            if (group == null)
                throw new NotFoundException(T("Group not found!"));

            return group;
        }

        public int GetStudentCount(int groupId, List<Student> students)
        {
            return students.Count(student => student.GroupId == groupId);
        }

        public Group DeleteGroup(int groupId, List<Student> students)
        {
            Group group = FindGroup(groupId);

            if (GetStudentCount(groupId, students) > 0)
                throw new ValidationException(T("The group still has students!"));

            _groups.Remove(group);

            return group;
        }

        public List<Group> GetGroupsWithFreeSeats(List<Student> students)
        {
            return GetAllGroups()
                .Where(group => GetStudentCount(group.Id, students) < group.MaxStudents)
                .ToList();
        }

        public Group Enroll(Student student, int groupId, List<Student> students)
        {
            if (student.GroupId != 0)
                throw new ValidationException(T("The student is already in a group!"));

            Group group = FindGroup(groupId);

            if (GetStudentCount(groupId, students) >= group.MaxStudents)
                throw new GroupFullException(T("Group {0} is full!", group.Name));

            student.GroupId = groupId;

            return group;
        }

        public Group? GetGroupOfStudent(Student student)
        {
            return _groups.FirstOrDefault(group => group.Id == student.GroupId);
        }

        public Course? GetCourseOfStudent(Student student)
        {
            Group? group = GetGroupOfStudent(student);

            if (group == null)
            {
                return null;
            }

            return _courses.FirstOrDefault(course => course.Id == group.CourseId);
        }
    }
}
