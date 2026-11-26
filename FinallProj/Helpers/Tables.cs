using FinallProj.Managers;
using FinallProj.Models;
using FinallProj.Models.Enums;
using Spectre.Console;
using static FinallProj.Helpers.Translator;

namespace FinallProj.Helpers
{
    internal static class Tables
    {
        private static Table CreateTable(string title)
        {
            Ui.Title(T(title));

            return new Table().Border(TableBorder.Rounded);
        }

        public static string StatusMarkup(PaymentStatus status)
        {
            if (status == PaymentStatus.Paid)
            {
                return $"[{Theme.Success}]{Markup.Escape(T("paid"))}[/]";
            }

            if (status == PaymentStatus.Overdue)
            {
                return $"[{Theme.Error}]{Markup.Escape(T("overdue"))}[/]";
            }

            return $"[{Theme.Warn}]{Markup.Escape(T("pending"))}[/]";
        }

        public static void ShowStudents(string title, List<Student> students)
        {
            Table table = CreateTable(title);
            table.AddColumn(T("Roll N"));
            table.AddColumn(T("Name"));
            table.AddColumn(T("Course"));
            table.AddColumn(T("Group"));
            table.AddColumn(T("Grade"));
            table.AddColumn(T("Status"));
            table.AddColumn(new TableColumn(T("Debt")).RightAligned());

            foreach (Student student in students)
            {
                Course? course = App.CourseManager.GetCourseOfStudent(student);
                Group? group = App.CourseManager.GetGroupOfStudent(student);

                string courseText = course == null ? "-" : Markup.Escape(course.Code);
                string groupText = group == null ? "-" : Markup.Escape(group.Name);
                string gradeText = student.IsGraded() ? student.Grade.ToString() : "-";

                string status = $"[{Theme.Muted}]{Markup.Escape(T("not graded"))}[/]";

                if (student.IsGraded())
                {
                    status = student.HasPassed()
                        ? $"[{Theme.Success}]{Markup.Escape(T("passed"))}[/]"
                        : $"[{Theme.Error}]{Markup.Escape(T("failed"))}[/]";
                }

                decimal debt = App.PaymentManager.GetOutstanding(student.Id, Clock.Today);
                string debtText = Format.Money(debt);

                if (App.PaymentManager.HasOverdue(student.Id, Clock.Today))
                {
                    debtText = $"[{Theme.Error}]{debtText}[/]";
                }

                table.AddRow(student.RollNumber.ToString(), Markup.Escape(student.Name), courseText, groupText, gradeText, status, debtText);
            }

            Ui.Write(table);
        }

        public static void ShowPayments(string title, List<Payment> payments)
        {
            Table table = CreateTable(title);
            table.AddColumn("#");
            table.AddColumn(T("Payment"));
            table.AddColumn(T("Due date"));
            table.AddColumn(new TableColumn(T("Amount")).RightAligned());
            table.AddColumn(new TableColumn(T("Late fee")).RightAligned());
            table.AddColumn(T("Status"));
            table.AddColumn(T("Paid on"));

            DateTime today = Clock.Today;

            foreach (Payment payment in payments)
            {
                PaymentStatus status = payment.GetStatus(today);

                string title2 = payment.Number == 0 ? T("Enrollment fee") : T("Installment {0}", payment.Number);

                decimal lateFee = payment.LateFee;

                if (status == PaymentStatus.Overdue)
                {
                    lateFee = PaymentManager.LateFee;
                }

                string paidOn = payment.IsPaid ? Format.Date(payment.PaidDate) : "-";

                table.AddRow(
                    payment.Number.ToString(),
                    Markup.Escape(title2),
                    Format.Date(payment.DueDate),
                    Format.Money(payment.Amount),
                    lateFee > 0 ? Format.Money(lateFee) : "-",
                    StatusMarkup(status),
                    paidOn);
            }

            Ui.Write(table);
        }

        public static void ShowCourses(List<Course> courses)
        {
            Table table = CreateTable("courses");
            table.AddColumn(T("Code"));
            table.AddColumn(T("Name"));
            table.AddColumn(new TableColumn(T("Tuition")).RightAligned());
            table.AddColumn(new TableColumn(T("Installments")).RightAligned());
            table.AddColumn(new TableColumn(T("Per month")).RightAligned());
            table.AddColumn(T("Lecturer"));
            table.AddColumn(new TableColumn(T("Groups")).RightAligned());

            foreach (Course course in courses)
            {
                int groupCount = App.CourseManager.GetAllGroups().Count(group => group.CourseId == course.Id);
                string lecturer = course.LecturerUsername == string.Empty ? "-" : Markup.Escape(course.LecturerUsername);

                table.AddRow(
                    Markup.Escape(course.Code),
                    Markup.Escape(course.Name),
                    Format.Money(course.Tuition),
                    course.Installments.ToString(),
                    Format.Money(course.GetMonthlyAmount()),
                    lecturer,
                    groupCount.ToString());
            }

            Ui.Write(table);
        }

        public static void ShowGroups(List<Group> groups)
        {
            List<Student> students = App.StudentManager.GetAllStudents();

            Table table = CreateTable("groups");
            table.AddColumn(T("Group"));
            table.AddColumn(T("Course"));
            table.AddColumn(T("Start date"));
            table.AddColumn(new TableColumn(T("Students")).RightAligned());
            table.AddColumn(new TableColumn(T("Free seats")).RightAligned());

            foreach (Group group in groups)
            {
                Course course = App.CourseManager.FindCourse(group.CourseId);
                int count = App.CourseManager.GetStudentCount(group.Id, students);
                int free = group.MaxStudents - count;

                string freeText = free > 0
                    ? $"[{Theme.Success}]{free}[/]"
                    : $"[{Theme.Error}]{T("full")}[/]";

                table.AddRow(
                    Markup.Escape(group.Name),
                    Markup.Escape(course.Code),
                    Format.Date(group.StartDate),
                    $"{count} / {group.MaxStudents}",
                    freeText);
            }

            Ui.Write(table);
        }

        public static void ShowUsers(List<User> users)
        {
            Table table = CreateTable("all users");
            table.AddColumn("ID");
            table.AddColumn(T("Role"));
            table.AddColumn(T("Name"));
            table.AddColumn(T("Username"));
            table.AddColumn(T("Details"));

            foreach (User user in users)
            {
                string details = "-";

                if (user is Lecturer lecturer)
                {
                    details = lecturer.Subject;
                }
                else if (user is Student student)
                {
                    details = $"N{student.RollNumber}";
                }

                table.AddRow(user.Id.ToString(), Markup.Escape(T(user.Role.ToString())), Markup.Escape(user.Name), Markup.Escape(user.Username), Markup.Escape(details));
            }

            Ui.Write(table);
        }
    }
}
