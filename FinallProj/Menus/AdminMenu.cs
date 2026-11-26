using FinallProj.Exceptions;
using FinallProj.Helpers;
using FinallProj.Managers;
using FinallProj.Models;
using FinallProj.Models.Enums;
using Spectre.Console;
using static FinallProj.Helpers.Translator;

namespace FinallProj.Menus
{
    internal static class AdminMenu
    {
        public static void FinanceSection()
        {
            List<MenuItem> items = new List<MenuItem>
            {
                new MenuItem("Financial dashboard", ShowDashboard),
                new MenuItem("Student payments", ShowStudentPayments),
                new MenuItem("Debtors (overdue payments)", ShowDebtors),
                new MenuItem("Export payments to CSV", ExportCsv),
                new MenuItem("Move the date forward (simulation)", MoveDate),
                new MenuItem("Reset the date to real today", ResetDate)
            };

            Ui.RunMenu("Finance", items);
        }

        private static void ShowDashboard()
        {
            DateTime today = Clock.Today;
            FinanceSummary summary = App.PaymentManager.GetSummary(today);

            if (summary.Expected == 0)
            {
                Ui.ShowWarning(T("There are no payments yet."));
                return;
            }

            Ui.Title($"{T("Financial dashboard")} - {Format.Date(today)}");

            Table table = new Table().Border(TableBorder.Rounded);
            table.AddColumn(T("Item"));
            table.AddColumn(new TableColumn(T("Amount")).RightAligned());

            table.AddRow(Markup.Escape(T("Expected in total")), Format.Money(summary.Expected));
            table.AddRow($"[{Theme.Success}]{Markup.Escape(T("Collected"))}[/]", Format.Money(summary.Collected));
            table.AddRow($"[{Theme.Warn}]{Markup.Escape(T("Not due yet"))}[/]", Format.Money(summary.Pending));
            table.AddRow($"[{Theme.Error}]{Markup.Escape(T("Overdue"))}[/]", Format.Money(summary.Overdue));
            table.AddRow($"[{Theme.Error}]{Markup.Escape(T("Late fees to collect"))}[/]", Format.Money(summary.LateFees));
            table.AddRow(Markup.Escape(T("Collection rate")), $"{summary.GetCollectionRate():F1}%");

            Ui.Write(table);

            BreakdownChart chart = new BreakdownChart().Width(60);
            chart.AddItem(T("Collected"), (double)summary.Collected, Theme.SuccessColor);
            chart.AddItem(T("Not due yet"), (double)summary.Pending, Theme.WarnColor);
            chart.AddItem(T("Overdue"), (double)(summary.Overdue + summary.LateFees), Theme.ErrorColor);

            AnsiConsole.WriteLine();
            Ui.Write(chart);

            ShowRevenueByCourse();
        }

        private static void ShowRevenueByCourse()
        {
            List<Student> students = App.StudentManager.GetAllStudents();

            var rows = App.PaymentManager.GetAllPayments()
                .Join(students, payment => payment.StudentId, student => student.Id, (payment, student) => new { payment, student })
                .Where(item => item.student.GroupId != 0)
                .GroupBy(item => App.CourseManager.GetCourseOfStudent(item.student)!.Code)
                .Select(group => new
                {
                    Code = group.Key,
                    Students = group.Select(item => item.student.Id).Distinct().Count(),
                    Collected = group.Where(item => item.payment.IsPaid).Sum(item => item.payment.Amount + item.payment.LateFee),
                    Total = group.Sum(item => item.payment.Amount)
                })
                .OrderBy(row => row.Code)
                .ToList();

            Table table = new Table().Border(TableBorder.Rounded).Title($"[{Theme.Title}]{Markup.Escape(T("Revenue by course"))}[/]");
            table.AddColumn(T("Course"));
            table.AddColumn(new TableColumn(T("Students")).RightAligned());
            table.AddColumn(new TableColumn(T("Collected")).RightAligned());
            table.AddColumn(new TableColumn(T("Expected in total")).RightAligned());

            foreach (var row in rows)
            {
                table.AddRow(Markup.Escape(row.Code), row.Students.ToString(), Format.Money(row.Collected), Format.Money(row.Total));
            }

            AnsiConsole.WriteLine();
            Ui.Write(table);
        }

        private static void ShowStudentPayments()
        {
            int rollNumber = Ui.ReadPositiveNumber("Enter the student's roll number:");
            Student student = App.StudentManager.FindByRollNumber(rollNumber);

            List<Payment> payments = App.PaymentManager.GetPaymentsOf(student.Id);

            if (payments.Count == 0)
            {
                Ui.ShowWarning(T("This student has no payment plan (not enrolled in a group)."));
                return;
            }

            Tables.ShowPayments(T("Payments of {0}", student.Name), payments);

            decimal outstanding = App.PaymentManager.GetOutstanding(student.Id, Clock.Today);
            Ui.Line($"{Markup.Escape(T("Left to pay"))}: [{Theme.Title}]{Format.Money(outstanding)}[/]");
        }

        private static void ShowDebtors()
        {
            DateTime today = Clock.Today;

            List<Student> debtors = App.StudentManager.GetAllStudents()
                .Where(student => App.PaymentManager.HasOverdue(student.Id, today))
                .OrderByDescending(student => App.PaymentManager.GetOutstanding(student.Id, today))
                .ToList();

            if (debtors.Count == 0)
            {
                Ui.ShowSuccess(T("No overdue payments. Everyone is on time!"));
                return;
            }

            Tables.ShowStudents(T("Debtors ({0})", debtors.Count), debtors);
        }

        private static void ExportCsv()
        {
            List<string> rows = new List<string>();
            DateTime today = Clock.Today;

            foreach (Payment payment in App.PaymentManager.GetAllPayments())
            {
                Student? student = App.StudentManager.GetAllStudents().FirstOrDefault(item => item.Id == payment.StudentId);

                if (student == null)
                {
                    continue;
                }

                Course? course = App.CourseManager.GetCourseOfStudent(student);
                string courseCode = course == null ? "-" : course.Code;
                string paidDate = payment.IsPaid ? Format.Date(payment.PaidDate) : "-";

                rows.Add($"{student.RollNumber},\"{student.Name}\",{courseCode},{payment.Number},{Format.Number(payment.Amount)},{Format.Date(payment.DueDate)},{payment.GetStatus(today)},{paidDate},{Format.Number(payment.LateFee)}");
            }

            string path = App.Files.ExportPaymentsCsv(rows);

            Ui.ShowSuccess(T("Exported {0} payments to {1}", rows.Count, path));
        }

        private static void MoveDate()
        {
            int days = Ui.ReadNumberInRange("How many days to move forward (1-365)?", 1, 365);

            Clock.DayOffset += days;
            App.SaveSettings();
            App.Log($"date moved: +{days} days -> {Format.Date(Clock.Today)}");

            Ui.ShowSuccess(T("The program date is now {0}.", Format.Date(Clock.Today)));
        }

        private static void ResetDate()
        {
            Clock.DayOffset = 0;
            App.SaveSettings();

            Ui.ShowSuccess(T("The program date is now {0}.", Format.Date(Clock.Today)));
        }

        public static void SecuritySection()
        {
            List<MenuItem> items = new List<MenuItem>
            {
                new MenuItem("View all users", ShowAllUsers),
                new MenuItem("Add lecturer", AddLecturer),
                new MenuItem("Delete lecturer", DeleteLecturer),
                new MenuItem("Reset a user's password", ResetPassword),
                new MenuItem("Registration codes", ShowRegistrationCodes),
                new MenuItem("Audit log", ShowAuditLog),
                new MenuItem("Load starter data", LoadStarterData),
                new MenuItem("Load academic starter data", LoadAcademicData)
            };

            Ui.RunMenu("Users and security", items);
        }

        private static void ShowRegistrationCodes()
        {
            Grid grid = new Grid();
            grid.AddColumn();
            grid.AddColumn();
            grid.AddRow(Markup.Escape(T("Lecturer")), $"[{Theme.Accent}]{Markup.Escape(UserManager.LecturerCode)}[/]");
            grid.AddRow(Markup.Escape(T("Admin")), $"[{Theme.Accent}]{Markup.Escape(UserManager.AdminCode)}[/]");

            Ui.Write(new Panel(grid).Header($"[{Theme.Title}]{Markup.Escape(T("Registration codes"))}[/]").Border(BoxBorder.Rounded).Padding(2, 1));

            if (!Ui.Ask(T("Change the codes?"), false))
            {
                return;
            }

            string lecturerCode = Ui.ReadText("Enter the new lecturer code:", 6);
            string adminCode = Ui.ReadText("Enter the new admin code:", 6);

            UserManager.SetCodes(lecturerCode, adminCode);
            App.SaveSettings();
            App.Log("registration codes changed");

            Ui.ShowSuccess(T("Codes updated!"));
        }

        private static void ShowAllUsers()
        {
            List<User> users = App.UserManager.GetAllUsers();

            if (users.Count == 0)
            {
                Ui.ShowWarning(T("No users yet."));
                return;
            }

            Tables.ShowUsers(users);
        }

        private static void AddLecturer()
        {
            string name = Ui.ReadText("Enter the lecturer's full name:", 2);
            string username = AuthMenu.ReadFreeUsername();
            string subject = Ui.ReadText("Enter the subject:", 2);
            string temporary = PasswordHelper.GenerateTemporary();

            App.UserManager.AddLecturer(name, username, temporary, subject, string.Empty, string.Empty);
            App.SaveAll();
            App.Log($"lecturer added: {username}");

            Ui.ShowSuccess(T("Lecturer added successfully!"));
            Ui.Line($"{Markup.Escape(T("Username"))}: [{Theme.Accent}]{Markup.Escape(username)}[/], {Markup.Escape(T("temporary password"))}: [{Theme.Success}]{Markup.Escape(temporary)}[/]");
        }

        private static void DeleteLecturer()
        {
            List<Lecturer> lecturers = App.UserManager.GetAllLecturers();

            if (lecturers.Count == 0)
            {
                Ui.ShowWarning(T("No lecturers yet."));
                return;
            }

            List<string> labels = lecturers.Select(lecturer => $"{lecturer.Name} ({lecturer.Username})").ToList();
            int choice = Ui.ChooseIndex("Choose a lecturer to delete", labels);
            Lecturer selected = lecturers[choice];

            if (App.CourseManager.GetAllCourses().Any(course => course.LecturerUsername == selected.Username))
                throw new ValidationException(T("This lecturer teaches a course. Delete or reassign the course first!"));

            if (!Ui.Confirm("Really delete this lecturer?"))
            {
                Ui.ShowWarning(T("Deletion cancelled."));
                return;
            }

            App.UserManager.DeleteLecturer(selected.Username);
            App.SaveAll();
            App.Log($"lecturer deleted: {selected.Username}");

            Ui.ShowSuccess(T("Lecturer {0} was deleted!", selected.Name));
        }

        private static void ResetPassword()
        {
            string username = Ui.ReadText("Enter the username to reset:", 3);
            string temporary = App.UserManager.ResetPassword(username);

            App.SaveAll();
            App.Log($"password reset by admin: {username}");

            Ui.ShowSuccess(T("Password was reset."));
            Ui.Line($"{Markup.Escape(T("temporary password"))}: [{Theme.Success}]{Markup.Escape(temporary)}[/]");
            Ui.ShowInfo(T("Give it to the user; they should change it after logging in."));
        }

        private static void ShowAuditLog()
        {
            List<string> lines = App.Files.ReadLog(25);

            if (lines.Count == 0)
            {
                Ui.ShowWarning(T("The log is empty."));
                return;
            }

            Panel panel = new Panel(Markup.Escape(string.Join("\n", lines)))
                .Header($"[{Theme.Title}]{Markup.Escape(T("Audit log (last 25 records)"))}[/]")
                .Border(BoxBorder.Rounded);

            Ui.Write(panel);
        }

        private static void LoadStarterData()
        {
            if (App.Courses.Count > 0)
                throw new ValidationException(T("Starter data can only be loaded when there are no courses yet!"));

            if (!Ui.Ask(T("Load starter courses, groups and 6 students?"), false))
            {
                Ui.ShowWarning(T("Cancelled."));
                return;
            }

            string starterPassword = "Step@2026";
            DateTime today = Clock.Today;

            if (!App.UserManager.UsernameExists("lecturer1"))
            {
                App.UserManager.AddLecturer("Giorgi Beridze", "lecturer1", starterPassword, "C# programming", string.Empty, string.Empty);
            }

            Course it = App.CourseManager.AddCourse("IT101", "Programming with C#", 3000m, 5, "lecturer1");
            Course design = App.CourseManager.AddCourse("DES201", "Graphic Design", 2400m, 4, string.Empty);
            Course english = App.CourseManager.AddCourse("ENG110", "Business English", 1800m, 3, string.Empty);

            Group it1 = App.CourseManager.AddGroup("IT-1", it.Id, 5, today.AddDays(-45));
            App.CourseManager.AddGroup("IT-2", it.Id, 20, today.AddDays(14));
            Group des1 = App.CourseManager.AddGroup("DES-1", design.Id, 12, today.AddDays(-45));
            Group eng1 = App.CourseManager.AddGroup("ENG-1", english.Id, 15, today.AddDays(-45));

            string[] names = { "Nino Kapanadze", "Luka Maisuradze", "Ana Gelashvili", "Saba Japaridze", "Mariam Chikovani", "Irakli Nozadze" };
            Group[] groups = { it1, it1, it1, des1, des1, eng1 };
            char[] grades = { 'A', 'B', 'N', 'C', 'F', 'B' };

            List<Student> students = new List<Student>();

            for (int i = 0; i < names.Length; i++)
            {
                Student student = App.StudentManager.AddStudent(names[i], string.Empty, starterPassword, string.Empty, string.Empty);

                if (grades[i] != 'N')
                {
                    student.UpdateGrade(grades[i]);
                }

                students.Add(student);
            }

            students[1].SetScholarship(20);

            for (int i = 0; i < students.Count; i++)
            {
                Course course = App.CourseManager.FindCourse(groups[i].CourseId);

                App.CourseManager.Enroll(students[i], groups[i].Id, students);
                App.PaymentManager.CreatePlan(students[i], groups[i], course, today.AddDays(-45));
            }

            PayInstallments(students[0], 3, today);
            PayInstallments(students[1], 2, today);
            PayInstallments(students[3], 3, today);
            PayInstallments(students[5], 4, today);

            App.SaveAll();
            AcademicSeed.Load();
            App.Log("starter data loaded");

            Ui.ShowSuccess(T("Starter data loaded! Logins: lecturer1 and s101-s106, password: Step@2026"));
        }

        private static void LoadAcademicData()
        {
            if (App.Courses.Count == 0)
            {
                Ui.ShowInfo(T("There are no courses, groups or students yet, so the main starter data is loaded together with the academic data."));
                LoadStarterData();
                return;
            }

            if (!Ui.Ask(T("Load starter lectures, news, points, assignments and exams?"), false))
            {
                Ui.ShowWarning(T("Cancelled."));
                return;
            }

            int count = AcademicSeed.Load();

            Ui.ShowSuccess(T("Academic starter data loaded ({0} lectures)!", count));
        }

        private static void PayInstallments(Student student, int count, DateTime today)
        {
            List<Payment> payments = App.PaymentManager.GetPaymentsOf(student.Id);

            for (int i = 0; i < count && i < payments.Count; i++)
            {
                if (!payments[i].IsPaid)
                {
                    App.PaymentManager.Pay(payments[i].Id, student, PaymentMethod.Card, payments[i].DueDate);
                }
            }
        }
    }
}
