using FinallProj.Models;
using FinallProj.Models.Enums;
using Spectre.Console;
using static FinallProj.Helpers.Translator;

namespace FinallProj.Helpers
{
    internal static class Screens
    {
        public static void ShowWelcome(User user)
        {
            Grid grid = new Grid();
            grid.AddColumn();
            grid.AddColumn();

            grid.AddRow($"[{Theme.Muted}]{Markup.Escape(T("Name"))}[/]", Markup.Escape(user.Name));
            grid.AddRow($"[{Theme.Muted}]{Markup.Escape(T("role"))}[/]", $"[{Theme.Accent}]{Markup.Escape(T(user.Role.ToString()))}[/]");
            grid.AddRow($"[{Theme.Muted}]{Markup.Escape(T("Date"))}[/]", Format.Date(Clock.Today));

            Panel panel = new Panel(grid)
                .Header($"[{Theme.Success}]{Markup.Escape(T("Welcome, {0}!", user.Name))}[/]")
                .Border(BoxBorder.Double)
                .Expand()
                .Padding(2, 1);

            Ui.Write(panel);

            if (user.Role == UserRole.Student)
            {
                return;
            }

            List<Student> students = App.StudentManager.GetAllStudents();
            int seats = App.Groups.Sum(group => group.MaxStudents);
            int free = seats - students.Count(student => student.GroupId != 0);

            Ui.Write(new Columns(
                SmallCard(T("Students"), students.Count, Theme.Accent),
                SmallCard(T("Courses"), App.Courses.Count, Theme.Title),
                SmallCard(T("Groups"), App.Groups.Count, Theme.Success),
                SmallCard(T("Free seats"), free, Theme.Warn)));
        }

        private static Panel SmallCard(string title, int number, string color)
        {
            Panel card = new Panel($"[bold {color}]{number}[/]")
                .Header(Markup.Escape(title))
                .Border(BoxBorder.Rounded)
                .Padding(2, 0);

            card.Width = 24;

            return card;
        }

        public static void ShowUniversityTree()
        {
            if (App.Courses.Count == 0)
            {
                Ui.ShowWarning(T("No courses yet."));
                return;
            }

            Tree root = new Tree($"[{Theme.Title}]{Markup.Escape(T("University structure"))}[/]");
            List<Student> students = App.StudentManager.GetAllStudents();

            foreach (Course course in App.CourseManager.GetAllCourses())
            {
                User? lecturer = App.UserManager.FindByUsername(course.LecturerUsername);
                string lecturerText = lecturer == null ? T("no lecturer") : lecturer.Name;

                TreeNode courseNode = root.AddNode($"[{Theme.Accent}]{Markup.Escape(course.Code)}[/] {Markup.Escape(course.Name)} [{Theme.Muted}]({Markup.Escape(lecturerText)}, {Format.Money(course.Tuition)})[/]");

                foreach (Group group in App.CourseManager.GetAllGroups().Where(item => item.CourseId == course.Id))
                {
                    List<Student> members = students.Where(student => student.GroupId == group.Id).ToList();

                    TreeNode groupNode = courseNode.AddNode($"[{Theme.Success}]{Markup.Escape(group.Name)}[/] [{Theme.Muted}]({members.Count}/{group.MaxStudents})[/]");

                    foreach (Student student in members)
                    {
                        groupNode.AddNode($"{Markup.Escape(student.Name)} [{Theme.Muted}]N{student.RollNumber}[/]");
                    }
                }
            }

            Ui.Write(root);
        }

        public static void ShowPaymentProgress(List<Payment> payments)
        {
            DateTime today = Clock.Today;

            int paid = payments.Count(payment => payment.GetStatus(today) == PaymentStatus.Paid);
            int overdue = payments.Count(payment => payment.GetStatus(today) == PaymentStatus.Overdue);
            int pending = payments.Count - paid - overdue;

            BreakdownChart chart = new BreakdownChart().Width(60);

            if (paid > 0)
            {
                chart.AddItem(T("paid"), paid, Theme.SuccessColor);
            }

            if (pending > 0)
            {
                chart.AddItem(T("pending"), pending, Theme.WarnColor);
            }

            if (overdue > 0)
            {
                chart.AddItem(T("overdue"), overdue, Theme.ErrorColor);
            }

            Ui.Write(chart);
        }

        public static void ShowPaymentCalendar(List<Payment> payments)
        {
            if (payments.Count == 0)
            {
                Ui.ShowWarning(T("No payments to show."));
                return;
            }

            List<DateTime> months = payments
                .Select(payment => new DateTime(payment.DueDate.Year, payment.DueDate.Month, 1))
                .Distinct()
                .OrderBy(month => month)
                .Take(6)
                .ToList();

            foreach (DateTime month in months)
            {
                Calendar calendar = new Calendar(month.Year, month.Month)
                    .Border(TableBorder.Rounded)
                    .HighlightStyle(new Style(Color.Black, Theme.TitleColor));

                foreach (Payment payment in payments.Where(item => item.DueDate.Year == month.Year && item.DueDate.Month == month.Month))
                {
                    calendar.AddCalendarEvent(payment.DueDate.Year, payment.DueDate.Month, payment.DueDate.Day);
                }

                Ui.Write(calendar);
            }

            Ui.Line($"[{Theme.Muted}]{Markup.Escape(T("Highlighted days are payment due dates."))}[/]");
        }
    }
}
