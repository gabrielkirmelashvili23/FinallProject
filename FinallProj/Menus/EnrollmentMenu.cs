using FinallProj.Helpers;
using FinallProj.Managers;
using FinallProj.Models;
using Spectre.Console;
using static FinallProj.Helpers.Translator;

namespace FinallProj.Menus
{
    internal static class EnrollmentMenu
    {
        public static void Enroll(Student student)
        {
            if (student.GroupId != 0)
            {
                Ui.ShowWarning(T("The student is already in a group!"));
                return;
            }

            List<Student> students = App.StudentManager.GetAllStudents();
            List<Group> groups = App.CourseManager.GetGroupsWithFreeSeats(students);

            if (groups.Count == 0)
            {
                Ui.ShowWarning(T("There are no groups with free seats yet."));
                return;
            }

            List<string> labels = new List<string>();

            foreach (Group group in groups)
            {
                Course course = App.CourseManager.FindCourse(group.CourseId);
                int free = group.MaxStudents - App.CourseManager.GetStudentCount(group.Id, students);

                labels.Add($"{group.Name} | {course.Code} {course.Name} | {Format.Money(course.Tuition)} | {Format.Date(group.StartDate)} | {free}");
            }

            labels.Add("Cancel");

            int choice = Ui.ChooseIndex("Choose a group (name | course | tuition | start | free seats)", labels);

            if (choice == groups.Count)
            {
                return;
            }

            Group selectedGroup = groups[choice];
            Course selectedCourse = App.CourseManager.FindCourse(selectedGroup.CourseId);

            ShowCostPreview(student, selectedCourse);

            if (!Ui.Ask(T("Enroll in this group?"), true))
            {
                Ui.ShowInfo(T("Enrollment cancelled."));
                return;
            }

            App.CourseManager.Enroll(student, selectedGroup.Id, students);

            List<Payment> plan = App.PaymentManager.CreatePlan(student, selectedGroup, selectedCourse, Clock.Today);

            App.SaveAll();
            App.Log($"enrolled: {student.Username} -> {selectedGroup.Name}");

            Ui.ShowSuccess(T("{0} is enrolled in group {1}!", student.Name, selectedGroup.Name));
            Ui.Line(Markup.Escape(T("Payment plan created: {0} payments. First due date: {1}.", plan.Count, Format.Date(plan[0].DueDate))));
        }

        private static void ShowCostPreview(Student student, Course course)
        {
            decimal net = App.PaymentManager.GetNetTuition(student, course);
            decimal monthly = Math.Round(net / course.Installments, 2);

            Table table = new Table().Border(TableBorder.Rounded).Title($"[{Theme.Title}]{Markup.Escape(T("Cost preview"))}[/]");
            table.AddColumn(T("Item"));
            table.AddColumn(new TableColumn(T("Amount")).RightAligned());

            table.AddRow(Markup.Escape(T("Tuition")), Format.Money(course.Tuition));
            table.AddRow(Markup.Escape(T("Scholarship")), $"{student.ScholarshipPercent}%");
            table.AddRow(Markup.Escape(T("Tuition after scholarship")), Format.Money(net));
            table.AddRow(Markup.Escape(T("Enrollment fee")), Format.Money(PaymentManager.EnrollmentFee));
            table.AddRow(Markup.Escape(T("Monthly payment ({0} months)", course.Installments)), Format.Money(monthly));

            Ui.Write(table);
        }

        public static void Unenroll(Student student)
        {
            if (student.GroupId == 0)
            {
                Ui.ShowWarning(T("The student is not in a group."));
                return;
            }

            App.PaymentManager.DeletePlan(student.Id);
            student.GroupId = 0;

            App.SaveAll();
            App.Log($"unenrolled: {student.Username}");

            Ui.ShowSuccess(T("The student was removed from the group."));
        }
    }
}
