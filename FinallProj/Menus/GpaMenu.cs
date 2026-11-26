using FinallProj.Exceptions;
using FinallProj.Helpers;
using FinallProj.Managers;
using FinallProj.Models;
using FinallProj.Models.Enums;
using Spectre.Console;
using static FinallProj.Helpers.Translator;

namespace FinallProj.Menus
{
    internal static class GpaMenu
    {
        public static void ShowStudent(Student student)
        {
            List<MenuItem> items = new List<MenuItem>
            {
                new MenuItem("Semesters and GPA", () => ShowSemesters(student)),
                new MenuItem("Academic years and year end", () => ShowYears(student)),
                new MenuItem("How GPA and year end work", ShowRules)
            };

            Ui.RunMenu("Semesters and GPA", items);
        }

        public static void ShowStaff(User user)
        {
            List<MenuItem> items = new List<MenuItem>
            {
                new MenuItem("Semesters and GPA of a student", () => ShowSemesters(Pickers.PickStudentByRoll(user))),
                new MenuItem("Academic years of a student", () => ShowYears(Pickers.PickStudentByRoll(user))),
                new MenuItem("Year results of all students", () => ShowYearTable(user)),
                new MenuItem("How GPA and year end work", ShowRules)
            };

            if (user.Role == UserRole.Admin)
            {
                items.Add(new MenuItem("Close an academic year", () => CloseYear(user)));
            }

            Ui.RunMenu("Semesters and GPA", items);
        }

        private static void ShowSemesters(Student student)
        {
            List<SemesterResult> results = App.AcademicManager.GetSemesterResults(student);

            if (results.Count == 0)
            {
                Ui.ShowInfo(T("No semester data yet for {0}.", student.Name));
                return;
            }

            Ui.Title($"{T("Semesters and GPA")} - {student.Name}");

            Table table = new Table().Border(TableBorder.Rounded);
            table.AddColumn(T("Semester"));
            table.AddColumn(new TableColumn(T("Points")).RightAligned());
            table.AddColumn(new TableColumn(T("Activity %")).RightAligned());
            table.AddColumn(new TableColumn(T("Exams %")).RightAligned());
            table.AddColumn(new TableColumn(T("Total %")).RightAligned());
            table.AddColumn(T("Letter"));
            table.AddColumn(new TableColumn("GPA").RightAligned());
            table.AddColumn(T("Final exam"));

            foreach (SemesterResult result in results)
            {
                string exams = result.ExamPercent == null ? "-" : $"{result.ExamPercent.Value:F1}";
                string label = Markup.Escape(result.Semester.ShortLabel) + (result.Provisional ? $" [{Theme.Muted}]*[/]" : string.Empty);

                table.AddRow(label, result.ActivityPoints.ToString(), $"{result.ActivityPercent:F1}", exams, $"{result.TotalPercent:F1}", result.Letter.ToString(), Views.GpaText(result.Gpa), Views.FinalStatusText(result.FinalStatus));
            }

            Ui.Write(table);

            foreach (SemesterResult result in results.Where(item => item.Exams.Count > 0))
            {
                Table exams = new Table().Border(TableBorder.Rounded).Title(Markup.Escape(result.Semester.Label));
                exams.AddColumn(T("Exam"));
                exams.AddColumn(T("Type"));
                exams.AddColumn(T("Attempts"));
                exams.AddColumn(new TableColumn(T("Best")).RightAligned());
                exams.AddColumn(new TableColumn("%").RightAligned());
                exams.AddColumn(T("Result"));

                foreach (ExamLine line in result.Exams)
                {
                    string state = line.Attempts == 0 ? Views.Colored(Theme.Muted, T("no result")) : line.Passed ? Views.Colored(Theme.Success, T("passed")) : Views.Colored(Theme.Error, T("failed"));

                    exams.AddRow(Markup.Escape(line.Exam.Title), Markup.Escape(Views.ExamKind(line.Exam)), line.Attempts.ToString(), line.Attempts == 0 ? "-" : $"{line.BestPoints}/{line.Exam.MaxPoints}", line.Attempts == 0 ? "-" : $"{line.Percent:F1}", state);
                }

                Ui.Write(exams);
            }

            if (results.Any(item => item.Provisional))
            {
                Ui.Line($"[{Theme.Muted}]* {Markup.Escape(T("provisional"))}[/]");
            }

            double cumulative = App.AcademicManager.CumulativeGpa(student);

            Ui.Line($"{Markup.Escape(T("Cumulative GPA (all semesters)"))}: {Views.GpaText(cumulative)} / 5.00   [{Theme.Muted}]{Markup.Escape(T("Pass level: {0}", AcademicManager.PassGpa.ToString("F2")))}[/]");

            foreach (SemesterResult result in results.Where(item => item.FinalStatus == FinalStatus.Stuck || item.FinalStatus == FinalStatus.MustRepeat))
            {
                Ui.ShowError(result.FinalStatus == FinalStatus.Stuck
                    ? T("Stuck in the final of {0}: pass the retake to continue.", result.Semester.Label)
                    : T("All attempts of the final of {0} are used: the semester must be repeated.", result.Semester.Label));
            }
        }

        private static void ShowYears(Student student)
        {
            List<YearResult> years = App.AcademicManager.GetYears(student);

            if (years.Count == 0)
            {
                Ui.ShowInfo(T("No academic year data yet for {0}.", student.Name));
                return;
            }

            Ui.Title($"{T("Academic years and year end")} - {student.Name}");

            Table table = new Table().Border(TableBorder.Rounded);
            table.AddColumn(T("Academic year"));
            table.AddColumn(T("Semesters"));
            table.AddColumn(new TableColumn(T("Year GPA")).RightAligned());
            table.AddColumn(T("Letter"));
            table.AddColumn(T("Year end result"));
            table.AddColumn(T("Closed"));

            foreach (YearResult year in years)
            {
                table.AddRow($"{year.StartYear}/{year.StartYear + 1}", year.Semesters.Count.ToString(), Views.GpaText(year.Gpa), year.Letter.ToString(), Views.OutcomeText(year.Outcome), year.IsClosed ? Views.Colored(Theme.Success, T("yes")) : Views.Colored(Theme.Muted, T("no")));
            }

            Ui.Write(table);

            YearResult last = years[years.Count - 1];
            ShowCarryOver(last);
        }

        private static void ShowCarryOver(YearResult year)
        {
            string carries;
            string resets = T("The points and the rating of the semester start from 0 in the new semester (the all-time points and the history stay).");

            switch (year.Outcome)
            {
                case YearOutcome.Promoted:
                    carries = T("Carried to the next year: your year GPA ({0}), your letter grade {1} (written to your profile when the admin closes the year), all your history and your scholarship.", year.Gpa.ToString("F2"), year.Letter);
                    break;
                case YearOutcome.StuckInFinal:
                    carries = T("You stay in the final: the year is not closed for you, your profile grade does not change, and you continue only after passing the retake.");
                    break;
                case YearOutcome.MustRepeat:
                    carries = T("You must repeat the semester: all {0} attempts of the final are used.", ExamManager.MaxAttempts);
                    break;
                case YearOutcome.LowGpa:
                    carries = T("The year GPA is below {0}: you are not promoted, but the history is kept.", AcademicManager.PassGpa.ToString("F2"));
                    break;
                default:
                    carries = T("The year {0}/{1} is still running. It ends on {2}.", year.StartYear, year.StartYear + 1, Format.Date(AcademicManager.YearEnd(year.StartYear)));
                    break;
            }

            Ui.Write(new Panel(Markup.Escape(carries + "\n\n" + resets)).Header($"[{Theme.Title}]{Markup.Escape(T("What happens at the year end"))}[/]").Border(BoxBorder.Rounded).Padding(2, 1));
        }

        private static void ShowRules()
        {
            string text =
                T("SEMESTER: the 1st semester is September 1 - January 31, the 2nd is February 1 - August 31.") + "\n\n" +
                T("ACTIVITY % = points from lecturers and assignments in the semester, where {0} points = 100% (cannot go above 100% or below 0%).", AcademicManager.ActivityTarget) + "\n" +
                T("EXAMS % = average of the best attempts of the exams (the final counts double).") + "\n" +
                T("TOTAL % = {0}% exams + {1}% activity. Until the first exam is graded, the total is the activity % only (provisional).", (int)(AcademicManager.ExamWeight * 100), (int)(AcademicManager.ActivityWeight * 100)) + "\n\n" +
                T("SEMESTER GPA = total % / 20, so 100% = 5.00. Letters: A 90+, B 80+, C 70+, D 60+, E 50+, F below 50.") + "\n" +
                T("YEAR GPA = average of the semester GPAs of the academic year.") + "\n" +
                T("CUMULATIVE GPA = average of all semesters so far.") + "\n\n" +
                T("EXAMS: pass level is {0}%. A failed normal exam can be retaken (up to {1} attempts in total, the best attempt counts).", ExamManager.PassPercent, ExamManager.MaxAttempts) + "\n" +
                T("A failed FINAL exam means the student is STUCK IN FINAL: the semester and the year stay open until a retake is passed. After {0} failed attempts the semester must be repeated.", ExamManager.MaxAttempts) + "\n\n" +
                T("AT THE YEAR END (after August 31): promoted if every final is passed and the year GPA is at least {0}. Then the admin closes the year: the letter of the year GPA becomes the student's grade, the history stays, the points of the new semester start from 0.", AcademicManager.PassGpa.ToString("F2"));

            Ui.Write(new Panel(Markup.Escape(text)).Header($"[{Theme.Title}]{Markup.Escape(T("How GPA and year end work"))}[/]").Border(BoxBorder.Rounded).Padding(2, 1));
        }

        private static void ShowYearTable(User user)
        {
            List<Student> students = Access.StudentsFor(user);

            if (students.Count == 0)
                throw new NotFoundException(T("No students yet."));

            Ui.Title(T("Year results of all students"));

            Table table = new Table().Border(TableBorder.Rounded);
            table.AddColumn(T("Roll N"));
            table.AddColumn(T("Student"));
            table.AddColumn(T("Academic year"));
            table.AddColumn(new TableColumn(T("Year GPA")).RightAligned());
            table.AddColumn(new TableColumn(T("Cumulative GPA")).RightAligned());
            table.AddColumn(T("Year end result"));

            foreach (Student student in students)
            {
                List<YearResult> years = App.AcademicManager.GetYears(student);

                if (years.Count == 0)
                {
                    table.AddRow(student.RollNumber.ToString(), Markup.Escape(student.Name), "-", "-", "-", Views.Colored(Theme.Muted, T("no data")));
                    continue;
                }

                YearResult last = years[years.Count - 1];
                table.AddRow(student.RollNumber.ToString(), Markup.Escape(student.Name), $"{last.StartYear}/{last.StartYear + 1}", Views.GpaText(last.Gpa), Views.GpaText(App.AcademicManager.CumulativeGpa(student)), Views.OutcomeText(last.Outcome));
            }

            Ui.Write(table);
        }

        private static void CloseYear(User user)
        {
            Semester current = Semester.Of(Clock.Today);
            List<string> labels = new List<string>();
            List<int> years = new List<int>();

            for (int year = current.StartYear; year >= current.StartYear - 3; year--)
            {
                years.Add(year);
                labels.Add($"{year}/{year + 1}  ({T("ends")} {Format.Date(AcademicManager.YearEnd(year))})");
            }

            int choice = Ui.ChooseIndex("Choose the academic year to close", labels, false, false, false);
            int startYear = years[choice];

            if (!Ui.Confirm("Close this academic year? Promoted students get the letter of their year GPA as the profile grade."))
            {
                Ui.ShowWarning(T("Cancelled."));
                return;
            }

            (int created, int updated, int promoted) = App.AcademicManager.CloseYear(user, startYear, App.StudentManager.GetAllStudents());
            App.SaveAll();
            App.Log($"year closed: {startYear}/{startYear + 1} created {created} updated {updated} promoted {promoted}");

            Ui.ShowSuccess(T("Year closed: {0} new records, {1} updated, {2} students promoted (grade updated).", created, updated, promoted));
        }
    }
}
