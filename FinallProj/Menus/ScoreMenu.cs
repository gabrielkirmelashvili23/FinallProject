using FinallProj.Exceptions;
using FinallProj.Helpers;
using FinallProj.Managers;
using FinallProj.Models;
using FinallProj.Models.Enums;
using Spectre.Console;
using static FinallProj.Helpers.Translator;

namespace FinallProj.Menus
{
    internal static class ScoreMenu
    {
        public static void ShowStaff(User user)
        {
            List<MenuItem> items = new List<MenuItem>
            {
                new MenuItem("Give points to a student", () => GiveToStudent(user)),
                new MenuItem("Give points to a whole group", () => GiveToGroup(user)),
                new MenuItem("Points history of a student", () => ShowHistoryOfStudent(user)),
                new MenuItem("Delete a points entry", () => DeleteEntry(user)),
                new MenuItem("Student rating", () => ShowRating(user))
            };

            Ui.RunMenu("Points and rating", items);
        }

        public static void ShowStudent(Student student)
        {
            List<MenuItem> items = new List<MenuItem>
            {
                new MenuItem("My points", () => ShowHistory(student)),
                new MenuItem("Student rating", () => ShowRating(student))
            };

            Ui.RunMenu("Points and rating", items);
        }

        private static string Signed(int points)
        {
            return points > 0 ? $"[{Theme.Success}]+{points}[/]" : $"[{Theme.Error}]{points}[/]";
        }

        private static void GiveToStudent(User user)
        {
            Student student = Pickers.PickStudentByRoll(user);
            int given = 0;
            bool again = true;

            while (again)
            {
                int points = Ui.ReadSignedNumber(T("Enter the points for {0} ({1} to {2}, negative to take away):", student.Name, -AcademicManager.MaxEntryPoints, AcademicManager.MaxEntryPoints), -AcademicManager.MaxEntryPoints, AcademicManager.MaxEntryPoints);
                string reason = Ui.ReadOptional("Enter the reason (or press Enter to skip):");
                DateTime date = Ui.ReadDate("Enter the date of the lesson (yyyy-MM-dd):", Clock.Today);

                App.AcademicManager.AddScore(user, student, points, reason, date);
                App.SaveAll();
                App.Log($"points: {user.Username} -> {student.Username} {points}");
                given++;

                Ui.ShowSuccess(T("Recorded! {0} now has {1} points in this semester.", student.Name, App.AcademicManager.Points(student.Id, Semester.Of(Clock.Today))));

                again = Ui.Ask(T("Add another entry for {0}?", student.Name), false);
            }

            Ui.ShowInfo(T("Entries recorded: {0}", given));
        }

        private static void GiveToGroup(User user)
        {
            Group group = Pickers.PickGroup(user);
            List<Student> students = App.StudentManager.GetAllStudents().Where(student => student.GroupId == group.Id).OrderBy(student => student.Name).ToList();

            if (students.Count == 0)
                throw new NotFoundException(T("This group has no students yet."));

            string reason = Ui.ReadOptional("Enter the reason for all entries (or press Enter to skip):");
            DateTime date = Ui.ReadDate("Enter the date of the lesson (yyyy-MM-dd):", Clock.Today);
            int count = 0;

            foreach (Student student in students)
            {
                string text = Ui.ReadOptional(T("Points for {0} (Enter = skip):", student.Name));

                if (text.Length == 0)
                {
                    continue;
                }

                if (!int.TryParse(text, out int points))
                {
                    Ui.ShowError(T("Not a number, skipped."));
                    continue;
                }

                try
                {
                    App.AcademicManager.AddScore(user, student, points, reason, date);
                    count++;
                }
                catch (AppException ex)
                {
                    Ui.ShowError($"{student.Name}: {ex.Message}");
                }
            }

            App.SaveAll();
            App.Log($"group points: {user.Username} -> {group.Name} x{count}");

            Ui.ShowSuccess(T("Entries recorded: {0}", count));
        }

        private static void ShowHistoryOfStudent(User user)
        {
            Student student = Pickers.PickStudentByRoll(user);

            ShowHistory(student);
        }

        private static void ShowHistory(Student student)
        {
            List<ScoreEntry> entries = App.AcademicManager.GetScores(student.Id);

            if (entries.Count == 0)
            {
                Ui.ShowInfo(T("{0} has no points yet.", student.Name));
                return;
            }

            Ui.Title($"{T("Points history")} - {student.Name}");

            Table table = new Table().Border(TableBorder.Rounded);
            table.AddColumn("N");
            table.AddColumn(T("Date"));
            table.AddColumn(new TableColumn(T("Points")).RightAligned());
            table.AddColumn(T("Reason"));
            table.AddColumn(T("Given by"));

            foreach (ScoreEntry entry in entries)
            {
                table.AddRow(entry.Id.ToString(), Format.Date(entry.Date), Signed(entry.Points), Markup.Escape(entry.Reason), Markup.Escape(Access.LecturerName(entry.LecturerUsername)));
            }

            Ui.Write(table);

            Semester current = Semester.Of(Clock.Today);

            Ui.Line($"{Markup.Escape(T("This semester"))} ({Markup.Escape(current.Label)}): [{Theme.Title}]{App.AcademicManager.Points(student.Id, current)}[/]   {Markup.Escape(T("All time"))}: [{Theme.Title}]{App.AcademicManager.Points(student.Id, null)}[/]   {Markup.Escape(T("Entries"))}: {entries.Count}");
        }

        private static void DeleteEntry(User user)
        {
            Student student = Pickers.PickStudentByRoll(user);
            List<ScoreEntry> entries = App.AcademicManager.GetScores(student.Id)
                .Where(entry => entry.SubmissionId == 0 && (user.Role == UserRole.Admin || entry.LecturerUsername == user.Username))
                .Take(30)
                .ToList();

            int index = Pickers.PickIndex("Choose an entry to delete", entries, entry => $"{Format.Date(entry.Date)}  {entry.Points:+#;-#}  {entry.Reason}");

            if (!Ui.Confirm("Really delete this entry?"))
            {
                Ui.ShowWarning(T("Deletion cancelled."));
                return;
            }

            App.AcademicManager.DeleteScore(user, entries[index].Id);
            App.SaveAll();
            App.Log($"points entry deleted: #{entries[index].Id} by {user.Username}");

            Ui.ShowSuccess(T("Entry deleted!"));
        }

        public static void ShowRating(User user)
        {
            List<Group> groups = Access.GroupsFor(user);

            if (groups.Count == 0)
                throw new NotFoundException(T("You have no groups yet."));

            List<Student> students;
            string scopeName;

            if (groups.Count == 1)
            {
                students = App.StudentManager.GetAllStudents().Where(student => student.GroupId == groups[0].Id).ToList();
                scopeName = groups[0].Name;
            }
            else
            {
                List<string> labels = groups.Select(group => group.Name).ToList();
                labels.Insert(0, T("All groups"));

                int choice = Ui.ChooseIndex("Choose a group", labels, false, false, false);

                if (choice == 0)
                {
                    students = Access.StudentsFor(user);
                    scopeName = T("All groups");
                }
                else
                {
                    students = App.StudentManager.GetAllStudents().Where(student => student.GroupId == groups[choice - 1].Id).ToList();
                    scopeName = groups[choice - 1].Name;
                }
            }

            if (students.Count == 0)
                throw new NotFoundException(T("This group has no students yet."));

            Semester current = Semester.Of(Clock.Today);

            int period = Ui.ChooseIndex("Choose the period", new List<string> { "This semester", "All time" });
            Semester? semester = period == 0 ? current : null;

            int order = Ui.ChooseIndex("Choose the order", new List<string> { "Highest points first", "Lowest points first" });

            List<RankRow> rows = App.AcademicManager.Rating(students, semester, order == 0);

            Ui.Title($"{T("Student rating")} - {scopeName} - {(semester == null ? T("All time") : current.Label)}");

            Table table = new Table().Border(TableBorder.Rounded);
            table.AddColumn(new TableColumn(T("Place")).RightAligned());
            table.AddColumn(T("Student"));
            table.AddColumn(T("Group"));
            table.AddColumn(new TableColumn(T("Points")).RightAligned());
            table.AddColumn(new TableColumn(T("Entries")).RightAligned());

            foreach (RankRow row in rows)
            {
                bool me = row.Student.Id == user.Id;
                string place = row.Position == 1 ? $"[{Theme.Title}]★ 1[/]" : row.Position.ToString();
                string name = me ? $"[{Theme.Title}]{Markup.Escape(row.Student.Name)} ({Markup.Escape(T("you"))})[/]" : Markup.Escape(row.Student.Name);

                table.AddRow(place, name, Markup.Escape(Access.GroupName(row.Student.GroupId)), row.Points.ToString(), row.Entries.ToString());
            }

            Ui.Write(table);
        }
    }
}
