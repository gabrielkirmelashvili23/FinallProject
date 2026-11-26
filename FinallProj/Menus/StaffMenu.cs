using FinallProj.Helpers;
using FinallProj.Managers;
using FinallProj.Models;
using FinallProj.Models.Enums;
using Spectre.Console;
using static FinallProj.Helpers.Translator;

namespace FinallProj.Menus
{
    internal static class StaffMenu
    {
        public static void Show(User user)
        {
            bool isAdmin = user.Role == UserRole.Admin;

            List<MenuItem> items = new List<MenuItem>
            {
                new MenuItem("Students", () => StudentsSection(isAdmin)),
                new MenuItem("Courses and groups", () => CoursesSection(isAdmin)),
                new MenuItem("Academic", () => AcademicMenu.Show(user)),
                new MenuItem("University news", () => NewsMenu.Show(user)),
                new MenuItem("University tree", Screens.ShowUniversityTree)
            };

            if (isAdmin)
            {
                items.Add(new MenuItem("Finance", AdminMenu.FinanceSection));
                items.Add(new MenuItem("Users and security", AdminMenu.SecuritySection));
            }

            items.Add(new MenuItem("My account", () => AccountMenu.Show(user)));

            Ui.RunMenu(isAdmin ? "Admin menu" : "Lecturer menu", items, "Log out");

            App.Log($"logout: {user.Username}");
            Ui.ShowWarning(T("You have logged out."));
        }

        private static void StudentsSection(bool isAdmin)
        {
            List<MenuItem> items = new List<MenuItem>
            {
                new MenuItem("Add student", AddStudent),
                new MenuItem("View all students", ShowAllStudents),
                new MenuItem("Search students", SearchStudents),
                new MenuItem("Update grade", UpdateGrade),
                new MenuItem("Enroll student in a group", EnrollStudent),
                new MenuItem("Statistics", ShowStatistics),
                new MenuItem("Create report card", CreateReport),
                new MenuItem("Delete student", DeleteStudent)
            };

            if (isAdmin)
            {
                items.Add(new MenuItem("Set scholarship", SetScholarship));
                items.Add(new MenuItem("Remove student from group", RemoveFromGroup));
            }

            Ui.RunMenu("Students", items);
        }

        private static Student AskStudent()
        {
            int rollNumber = Ui.ReadPositiveNumber("Enter the student's roll number:");

            return App.StudentManager.FindByRollNumber(rollNumber);
        }

        private static void AddStudent()
        {
            string name = Ui.ReadText("Enter the student's full name:", 2);
            string temporary = PasswordHelper.GenerateTemporary();

            Student student = App.StudentManager.AddStudent(name, string.Empty, temporary, string.Empty, string.Empty);

            App.SaveAll();
            App.Log($"student added by staff: {student.Username}");

            Ui.ShowSuccess(T("Student added successfully!"));
            Ui.Line($"{Markup.Escape(T("Login data"))} -> {Markup.Escape(T("Username"))}: [{Theme.Accent}]{Markup.Escape(student.Username)}[/], {Markup.Escape(T("temporary password"))}: [{Theme.Success}]{Markup.Escape(temporary)}[/]");
            Ui.ShowInfo(T("The student should change the password after the first login."));

            if (Ui.Ask(T("Enroll in a group now?"), false))
            {
                EnrollmentMenu.Enroll(student);
            }
        }

        private static void ShowAllStudents()
        {
            List<Student> students = App.StudentManager.GetAllStudents();

            if (students.Count == 0)
            {
                Ui.ShowWarning(T("No students yet."));
                return;
            }

            Tables.ShowStudents(T("All students ({0})", students.Count), students);
        }

        private static void SearchStudents()
        {
            string text = Ui.ReadText("Enter a roll number or a part of the name:", 1);
            List<Student> found;

            if (int.TryParse(text, out int rollNumber))
            {
                found = new List<Student> { App.StudentManager.FindByRollNumber(rollNumber) };
            }
            else
            {
                found = App.StudentManager.SearchByName(text);
            }

            if (found.Count == 0)
            {
                Ui.ShowWarning(T("Nothing found."));
                return;
            }

            Tables.ShowStudents(T("Search results"), found);
        }

        private static void UpdateGrade()
        {
            Student student = AskStudent();
            string current = student.IsGraded() ? student.Grade.ToString() : "-";

            Ui.Line(Markup.Escape(T("Current grade of {0}: {1}", student.Name, current)));

            char newGrade = Ui.ReadGrade("Choose the new grade");
            App.StudentManager.UpdateGrade(student.RollNumber, newGrade);
            App.SaveAll();
            App.Log($"grade updated: {student.Username} -> {newGrade}");

            Ui.ShowSuccess(T("Grade updated!"));
            Tables.ShowStudents(T("Updated student"), new List<Student> { student });

            if (App.Files.ReportExists(student.RollNumber))
            {
                CreateReportFor(student);
                Ui.ShowInfo(T("The report file was updated with the new grade."));
            }
        }

        private static void EnrollStudent()
        {
            Student student = AskStudent();
            EnrollmentMenu.Enroll(student);
        }

        private static void RemoveFromGroup()
        {
            Student student = AskStudent();

            if (!Ui.Confirm("Really remove this student from the group?"))
            {
                Ui.ShowWarning(T("Cancelled."));
                return;
            }

            EnrollmentMenu.Unenroll(student);
        }

        private static void SetScholarship()
        {
            Student student = AskStudent();

            int percent = Ui.ReadNumberInRange("Enter the scholarship percent (0-100):", 0, 100);
            student.SetScholarship(percent);

            Course? course = App.CourseManager.GetCourseOfStudent(student);

            if (course != null)
            {
                App.PaymentManager.RecalculatePlan(student, course);
            }

            App.SaveAll();
            App.Log($"scholarship: {student.Username} -> {percent}%");

            Ui.ShowSuccess(T("Scholarship saved. Unpaid installments were recalculated."));
        }

        private static void DeleteStudent()
        {
            Student student = AskStudent();
            Tables.ShowStudents(T("Student to delete"), new List<Student> { student });

            if (!Ui.Confirm("Really delete this student?"))
            {
                Ui.ShowWarning(T("Deletion cancelled."));
                return;
            }

            App.StudentManager.DeleteStudent(student.RollNumber);
            App.PaymentManager.RemoveAllOf(student.Id);
            App.RemoveStudentData(student.Id);
            App.SaveAll();
            App.Log($"student deleted: {student.Username}");

            Ui.ShowSuccess(T("Student {0} was deleted!", student.Name));

            if (App.Files.DeleteReport(student.RollNumber))
            {
                Ui.ShowInfo(T("The student's report file was deleted."));
            }
        }

        private static void CreateReport()
        {
            Student student = AskStudent();
            string path = CreateReportFor(student);

            Ui.ShowSuccess(T("Report file created: {0}", path));
            Ui.ShowReport(App.Files.ReadReport(student.RollNumber));
        }

        private static string CreateReportFor(Student student)
        {
            Course? course = App.CourseManager.GetCourseOfStudent(student);
            Group? group = App.CourseManager.GetGroupOfStudent(student);
            decimal outstanding = App.PaymentManager.GetOutstanding(student.Id, Clock.Today);

            string courseName = course == null ? "-" : course.ToString();
            string groupName = group == null ? "-" : group.Name;

            return App.Files.CreateReport(student, courseName, groupName, outstanding);
        }

        private static void ShowStatistics()
        {
            List<Student> students = App.StudentManager.GetAllStudents();
            List<Student> graded = App.StudentManager.GetGradedStudents();

            if (students.Count == 0)
            {
                Ui.ShowWarning(T("No students for statistics."));
                return;
            }

            Ui.Title(T("Statistics"));
            Ui.Line(Markup.Escape(T("Number of students: {0}", students.Count)));

            if (graded.Count == 0)
            {
                Ui.ShowWarning(T("Nobody has been graded yet."));
                return;
            }

            double average = App.StudentManager.GetAverageGradePoints();
            Ui.Line($"{Markup.Escape(T("Average points"))}: [bold]{average:F2}[/] [{Theme.Muted}](A = 5 ... F = 0)[/]");

            BarChart chart = new BarChart().Label($"[{Theme.Title}]{Markup.Escape(T("Grade distribution"))}[/]").Width(50);

            foreach (IGrouping<char, Student> group in App.StudentManager.GetStudentsGroupedByGrade())
            {
                Color color = Theme.SuccessColor;

                if (group.Key == 'F')
                {
                    color = Theme.ErrorColor;
                }

                chart.AddItem(group.Key.ToString(), group.Count(), color);
            }

            AnsiConsole.WriteLine();
            Ui.Write(chart);

            List<Student> topStudents = App.StudentManager.GetTopStudents(3);

            Table topTable = new Table().Title($"[{Theme.Title}]{Markup.Escape(T("Top 3 students"))}[/]");
            topTable.AddColumn("#");
            topTable.AddColumn(T("Name"));
            topTable.AddColumn(T("Grade"));

            for (int i = 0; i < topStudents.Count; i++)
            {
                topTable.AddRow((i + 1).ToString(), Markup.Escape(topStudents[i].Name), topStudents[i].Grade.ToString());
            }

            AnsiConsole.WriteLine();
            Ui.Write(topTable);

            List<Student> failedStudents = App.StudentManager.GetFailedStudents();

            Ui.Line($"[{Theme.Success}]{Markup.Escape(T("passed: {0}", App.StudentManager.GetPassedStudents().Count))}[/], [{Theme.Error}]{Markup.Escape(T("failed: {0}", failedStudents.Count))}[/]");

            foreach (Student student in failedStudents)
            {
                Ui.Line($"   [{Theme.Error}]{Markup.Escape(T("failed"))}:[/] {Markup.Escape(student.Name)} (N{student.RollNumber})");
            }
        }

        private static void CoursesSection(bool isAdmin)
        {
            List<MenuItem> items = new List<MenuItem>
            {
                new MenuItem("View courses", ShowCourses),
                new MenuItem("View groups", ShowGroups)
            };

            if (isAdmin)
            {
                items.Add(new MenuItem("Add course", AddCourse));
                items.Add(new MenuItem("Delete course", DeleteCourse));
                items.Add(new MenuItem("Add group", AddGroup));
                items.Add(new MenuItem("Delete group", DeleteGroup));
            }

            Ui.RunMenu("Courses and groups", items);
        }

        private static void ShowCourses()
        {
            List<Course> courses = App.CourseManager.GetAllCourses();

            if (courses.Count == 0)
            {
                Ui.ShowWarning(T("No courses yet."));
                return;
            }

            Tables.ShowCourses(courses);
        }

        private static void ShowGroups()
        {
            List<Group> groups = App.CourseManager.GetAllGroups();

            if (groups.Count == 0)
            {
                Ui.ShowWarning(T("No groups yet."));
                return;
            }

            Tables.ShowGroups(groups);
        }

        private static void AddCourse()
        {
            string code = Ui.ReadText("Enter the course code (e.g. IT101):", 2);
            string name = Ui.ReadText("Enter the course name:", 2);
            decimal tuition = Ui.ReadMoney("Enter the tuition for one semester (GEL):");
            int installments = Ui.ReadNumberInRange("In how many monthly installments is it paid (1-12)?", 1, 12);

            string lecturer = ChooseLecturer();

            Course course = App.CourseManager.AddCourse(code, name, tuition, installments, lecturer);
            App.SaveAll();
            App.Log($"course added: {course.Code}");

            Ui.ShowSuccess(T("Course {0} added!", course.ToString()));
        }

        private static string ChooseLecturer()
        {
            List<Lecturer> lecturers = App.UserManager.GetAllLecturers();

            if (lecturers.Count == 0)
            {
                return string.Empty;
            }

            List<string> labels = lecturers.Select(lecturer => $"{lecturer.Name} ({lecturer.Username})").ToList();
            labels.Add("No lecturer yet");

            int choice = Ui.ChooseIndex("Choose the lecturer of this course", labels);

            if (choice == lecturers.Count)
            {
                return string.Empty;
            }

            return lecturers[choice].Username;
        }

        private static Course ChooseCourse()
        {
            List<Course> courses = App.CourseManager.GetAllCourses();

            if (courses.Count == 0)
                throw new Exceptions.NotFoundException(T("No courses yet."));

            List<string> labels = courses.Select(course => course.ToString()).ToList();
            int choice = Ui.ChooseIndex("Choose a course", labels);

            return courses[choice];
        }

        private static void DeleteCourse()
        {
            Course course = ChooseCourse();

            if (!Ui.Confirm("Really delete this course?"))
            {
                Ui.ShowWarning(T("Deletion cancelled."));
                return;
            }

            App.CourseManager.DeleteCourse(course.Id);
            App.SaveAll();
            App.Log($"course deleted: {course.Code}");

            Ui.ShowSuccess(T("Course deleted!"));
        }

        private static void AddGroup()
        {
            Course course = ChooseCourse();

            string name = Ui.ReadText("Enter the group name (e.g. IT-1):", 2);
            int maxStudents = Ui.ReadNumberInRange("Maximum number of students in the group:", 1, 100);
            DateTime startDate = Ui.ReadDate("Start date (yyyy-MM-dd):", Clock.Today);

            Group group = App.CourseManager.AddGroup(name, course.Id, maxStudents, startDate);
            App.SaveAll();
            App.Log($"group added: {group.Name}");

            Ui.ShowSuccess(T("Group {0} added!", group.Name));
        }

        private static void DeleteGroup()
        {
            List<Group> groups = App.CourseManager.GetAllGroups();

            if (groups.Count == 0)
                throw new Exceptions.NotFoundException(T("No groups yet."));

            List<string> labels = groups.Select(group => group.Name).ToList();
            int choice = Ui.ChooseIndex("Choose a group", labels);

            if (!Ui.Confirm("Really delete this group?"))
            {
                Ui.ShowWarning(T("Deletion cancelled."));
                return;
            }

            App.CourseManager.DeleteGroup(groups[choice].Id, App.StudentManager.GetAllStudents());
            App.RemoveGroupData(groups[choice].Id);
            App.SaveAll();
            App.Log($"group deleted: {groups[choice].Name}");

            Ui.ShowSuccess(T("Group deleted!"));
        }
    }
}
