using FinallProj.Exceptions;
using FinallProj.Helpers;
using FinallProj.Managers;
using FinallProj.Models;
using FinallProj.Models.Enums;
using Spectre.Console;
using static FinallProj.Helpers.Translator;

namespace FinallProj.Menus
{
    internal static class ExamMenu
    {
        public static void ShowStudent(Student student)
        {
            if (student.GroupId == 0)
            {
                Ui.ShowWarning(T("You are not enrolled in a group yet. Choose 'Enroll in a group' first."));
                return;
            }

            ShowStatusBanner(student);

            List<MenuItem> items = new List<MenuItem>
            {
                new MenuItem("Upcoming exams", () => ShowUpcoming(student)),
                new MenuItem("My exam results", () => ShowMyResults(student))
            };

            Ui.RunMenu("Exams", items);
        }

        public static void ShowStaff(User user)
        {
            List<MenuItem> items = new List<MenuItem>
            {
                new MenuItem("Upcoming exams", () => ShowUpcoming(user)),
                new MenuItem("Schedule an exam", () => Schedule(user)),
                new MenuItem("Schedule a retake", () => ScheduleRetake(user)),
                new MenuItem("Record exam results", () => Record(user)),
                new MenuItem("Exam results of a student", () => ShowResultsOfStudent(user)),
                new MenuItem("Move an exam", () => Move(user)),
                new MenuItem("Delete an exam", () => Delete(user))
            };

            Ui.RunMenu("Exams", items);
        }

        private static void ShowStatusBanner(Student student)
        {
            Semester semester = Semester.Of(Clock.Today);

            foreach (Semester item in App.AcademicManager.GetSemesters(student))
            {
                FinalStatus status = App.ExamManager.FinalStatusOf(student, item);

                if (status == FinalStatus.Stuck)
                {
                    Ui.ShowError(T("You are STUCK IN THE FINAL of {0}: you failed it and need a retake!", item.Label));
                }
                else if (status == FinalStatus.MustRepeat)
                {
                    Ui.ShowError(T("You failed the final of {0} in all attempts: the semester must be repeated!", item.Label));
                }
            }
        }

        private static void ShowUpcoming(User user)
        {
            List<int> groupIds = Access.GroupsFor(user).Select(group => group.Id).ToList();
            List<Exam> exams = App.ExamManager.GetForGroups(groupIds, Clock.Today);

            if (exams.Count == 0)
            {
                Ui.ShowInfo(T("No exams are scheduled."));
                return;
            }

            Ui.Title(T("Upcoming exams"));

            Table table = new Table().Border(TableBorder.Rounded);
            table.AddColumn(T("Date"));
            table.AddColumn(T("Time"));
            table.AddColumn(T("Exam"));
            table.AddColumn(T("Type"));

            if (user.Role != UserRole.Student)
            {
                table.AddColumn(T("Group"));
            }

            table.AddColumn(T("Where"));
            table.AddColumn(T("Details"));
            table.AddColumn(new TableColumn(T("Days left")).RightAligned());

            foreach (Exam exam in exams)
            {
                int days = (exam.Date.Date - Clock.Today).Days;
                string left = days == 0 ? $"[{Theme.Warn}]{Markup.Escape(T("today"))}[/]" : days.ToString();
                string kind = exam.IsFinal ? $"[{Theme.Error}]{Markup.Escape(Views.ExamKind(exam))}[/]" : Markup.Escape(Views.ExamKind(exam));

                List<string> cells = new List<string> { Format.Date(exam.Date), exam.StartTime, Markup.Escape(exam.Title), kind };

                if (user.Role != UserRole.Student)
                {
                    cells.Add(Markup.Escape(Access.GroupName(exam.GroupId)));
                }

                cells.Add(Markup.Escape(exam.Place));
                cells.Add(Markup.Escape(exam.Details.Length == 0 ? "-" : exam.Details));
                cells.Add(left);

                table.AddRow(cells.ToArray());
            }

            Ui.Write(table);
        }

        private static void ShowMyResults(Student student)
        {
            ShowResults(student);
        }

        private static void ShowResultsOfStudent(User user)
        {
            Student student = Pickers.PickStudentByRoll(user);

            ShowResults(student);
        }

        private static void ShowResults(Student student)
        {
            List<Exam> roots = App.ExamManager.GetAll().Where(exam => exam.RetakeOf == 0 && exam.GroupId == student.GroupId).ToList();
            bool any = false;

            Ui.Title($"{T("Exam results")} - {student.Name}");

            Table table = new Table().Border(TableBorder.Rounded);
            table.AddColumn(T("Exam"));
            table.AddColumn(T("Type"));
            table.AddColumn(T("Date"));
            table.AddColumn(T("Attempts"));
            table.AddColumn(new TableColumn(T("Best")).RightAligned());
            table.AddColumn(T("Result"));

            foreach (Exam root in roots)
            {
                List<ExamResult> attempts = App.ExamManager.AttemptsOf(root.Id, student.Id);
                string result;

                if (attempts.Count == 0)
                {
                    result = root.Date.Date > Clock.Today ? Views.Colored(Theme.Muted, T("not yet")) : Views.Colored(Theme.Warn, T("no result"));
                }
                else if (App.ExamManager.HasPassed(root.Id, student.Id))
                {
                    result = Views.Colored(Theme.Success, T("passed"));
                }
                else if (attempts.Count >= ExamManager.MaxAttempts)
                {
                    result = Views.Colored(Theme.Error, root.IsFinal ? T("failed all attempts, repeat the semester") : T("failed all attempts"));
                }
                else
                {
                    result = Views.Colored(Theme.Error, root.IsFinal ? T("STUCK IN FINAL (retake needed)") : T("failed, retake possible"));
                }

                string best = attempts.Count == 0 ? "-" : $"{App.ExamManager.BestPoints(root.Id, student.Id)}/{root.MaxPoints}";

                table.AddRow(Markup.Escape(root.Title), Markup.Escape(Views.ExamKind(root)), Format.Date(root.Date), attempts.Count.ToString(), best, result);
                any = true;
            }

            if (!any)
            {
                Ui.ShowInfo(T("No exams for this group yet."));
                return;
            }

            Ui.Write(table);

            List<ExamResult> history = App.ExamManager.GetAllResults().Where(result => result.StudentId == student.Id).OrderBy(result => result.Date).ThenBy(result => result.Id).ToList();

            if (history.Count > 0)
            {
                Table attemptsTable = new Table().Border(TableBorder.Rounded).Title(Markup.Escape(T("Attempt history")));
                attemptsTable.AddColumn(T("Exam"));
                attemptsTable.AddColumn(T("Attempt"));
                attemptsTable.AddColumn(T("Date"));
                attemptsTable.AddColumn(new TableColumn(T("Points")).RightAligned());

                foreach (ExamResult result in history)
                {
                    Exam exam = App.ExamManager.Find(result.ExamId);
                    attemptsTable.AddRow(Markup.Escape(exam.Title), result.Attempt.ToString(), Format.Date(result.Date), $"{result.Points}/{exam.MaxPoints}");
                }

                Ui.Write(attemptsTable);
            }
        }

        private static void Schedule(User user)
        {
            Group group = Pickers.PickGroup(user);

            string title = Ui.ReadText("Enter the exam title:", 3);
            bool isFinal = Ui.Ask(T("Is this the FINAL exam of the semester?"), false);
            DateTime date = Ui.ReadDate("Enter the exam date (yyyy-MM-dd):", Clock.Today.AddDays(7));
            string time = Ui.ReadTime("Enter the start time (HH:mm):", "10:00");
            string place = Ui.ReadText("Enter the room or link:", 2);
            string details = Ui.ReadOptional("Enter the details (topics, rules) or press Enter to skip:");
            int max = Ui.ReadNumberInRange("Enter the maximum points:", 10, 1000);

            Exam exam = App.ExamManager.Add(user, group.Id, title, date, time, place, details, isFinal, max);
            App.SaveAll();
            App.Log($"exam scheduled: #{exam.Id} {exam.Title} for {group.Name}");

            Ui.ShowSuccess(T("Exam scheduled! Students of {0} can see it now.", group.Name));
            Ui.ShowInfo(T("Pass level: {0}% of the maximum points, {1} attempts in total.", ExamManager.PassPercent, ExamManager.MaxAttempts));
        }

        private static Exam ChooseRoot(User user)
        {
            List<int> groupIds = Access.GroupsFor(user).Select(group => group.Id).ToList();
            List<Exam> roots = App.ExamManager.GetAll().Where(exam => exam.RetakeOf == 0 && groupIds.Contains(exam.GroupId)).OrderByDescending(exam => exam.Date).Take(40).ToList();

            int index = Pickers.PickIndex("Choose the original exam", roots, Views.ExamLabel);

            return roots[index];
        }

        private static void ScheduleRetake(User user)
        {
            Exam root = ChooseRoot(user);
            List<Student> waiting = App.ExamManager.StudentsNeedingRetake(root);

            if (waiting.Count == 0)
                throw new ValidationException(T("Nobody needs a retake of this exam!"));

            Ui.Line($"{Markup.Escape(T("Students who need a retake"))}: [{Theme.Warn}]{Markup.Escape(string.Join(", ", waiting.Select(student => student.Name)))}[/]");

            DateTime date = Ui.ReadDate("Enter the retake date (yyyy-MM-dd):", Clock.Today.AddDays(7));
            string time = Ui.ReadTime("Enter the start time (HH:mm):", "10:00");
            string place = Ui.ReadText("Enter the room or link:", 2);
            string details = Ui.ReadOptional("Enter the details or press Enter to skip:");

            Exam retake = App.ExamManager.AddRetake(user, root.Id, date, time, place, details);
            App.SaveAll();
            App.Log($"retake scheduled: #{retake.Id} of #{root.Id}");

            Ui.ShowSuccess(T("Retake scheduled!"));
        }

        private static void Record(User user)
        {
            List<int> groupIds = Access.GroupsFor(user).Select(group => group.Id).ToList();
            List<Exam> exams = App.ExamManager.GetAll()
                .Where(exam => groupIds.Contains(exam.GroupId) && exam.Date.Date <= Clock.Today && App.ExamManager.StudentsWaitingFor(exam).Count > 0)
                .OrderByDescending(exam => exam.Date)
                .Take(40)
                .ToList();

            int index = Pickers.PickIndex("Choose the exam", exams, Views.ExamLabel);
            Exam exam = exams[index];

            bool again = true;

            while (again)
            {
                List<Student> waiting = App.ExamManager.StudentsWaitingFor(exam);

                if (waiting.Count == 0)
                {
                    Ui.ShowInfo(T("All results of this exam are recorded."));
                    return;
                }

                int studentIndex = Ui.ChooseIndex("Choose a student", waiting.Select(student => $"{student.RollNumber}  {student.Name}").ToList());
                Student student = waiting[studentIndex];
                int points = Ui.ReadNumberInRange(T("Enter the points of {0} (0-{1}):", student.Name, exam.MaxPoints), 0, exam.MaxPoints);

                ExamResult result = App.ExamManager.RecordResult(user, exam, student, points);
                App.SaveAll();
                App.Log($"exam result: {student.Username} #{exam.Id} {points}/{exam.MaxPoints} attempt {result.Attempt}");

                ShowConsequence(exam, student);

                again = Ui.Ask(T("Record another result?"), true);
            }
        }

        private static void ShowConsequence(Exam exam, Student student)
        {
            Exam root = App.ExamManager.Find(exam.RootId);
            List<ExamResult> attempts = App.ExamManager.AttemptsOf(root.Id, student.Id);

            if (App.ExamManager.HasPassed(root.Id, student.Id))
            {
                Ui.ShowSuccess(T("{0} passed the exam (attempt {1}).", student.Name, attempts.Count));
                return;
            }

            if (attempts.Count >= ExamManager.MaxAttempts)
            {
                Ui.ShowError(root.IsFinal
                    ? T("{0} failed the final in all {1} attempts: the semester must be repeated!", student.Name, ExamManager.MaxAttempts)
                    : T("{0} failed the exam in all {1} attempts.", student.Name, ExamManager.MaxAttempts));
                return;
            }

            if (root.IsFinal)
            {
                Ui.ShowError(T("{0} failed the FINAL and is stuck in it. The semester and the year stay open until a retake is passed.", student.Name));
            }
            else
            {
                Ui.ShowWarning(T("{0} failed the exam. A retake is possible (attempts left: {1}).", student.Name, ExamManager.MaxAttempts - attempts.Count));
            }
        }

        private static void Move(User user)
        {
            List<int> groupIds = Access.GroupsFor(user).Select(group => group.Id).ToList();
            List<Exam> exams = App.ExamManager.GetForGroups(groupIds, Clock.Today);

            int index = Pickers.PickIndex("Choose the exam", exams, Views.ExamLabel);
            Exam exam = exams[index];

            DateTime date = Ui.ReadDate("Enter the new date (yyyy-MM-dd):", exam.Date);
            string time = Ui.ReadTime("Enter the new start time (HH:mm):", exam.StartTime);
            string place = Ui.ReadText("Enter the room or link:", 2);

            App.ExamManager.Reschedule(user, exam.Id, date, time, place);
            App.SaveAll();
            App.Log($"exam moved: #{exam.Id}");

            Ui.ShowSuccess(T("Exam moved!"));
        }

        private static void Delete(User user)
        {
            List<int> groupIds = Access.GroupsFor(user).Select(group => group.Id).ToList();
            List<Exam> exams = App.ExamManager.GetAll().Where(exam => groupIds.Contains(exam.GroupId)).OrderByDescending(exam => exam.Date).Take(40).ToList();

            int index = Pickers.PickIndex("Choose the exam", exams, Views.ExamLabel);

            if (!Ui.Confirm("Really delete this exam?"))
            {
                Ui.ShowWarning(T("Deletion cancelled."));
                return;
            }

            App.ExamManager.Delete(user, exams[index].Id);
            App.SaveAll();
            App.Log($"exam deleted: #{exams[index].Id}");

            Ui.ShowSuccess(T("Exam deleted!"));
        }
    }
}
