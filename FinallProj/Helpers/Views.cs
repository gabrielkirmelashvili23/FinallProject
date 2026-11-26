using FinallProj.Models;
using FinallProj.Models.Enums;
using Spectre.Console;
using static FinallProj.Helpers.Translator;

namespace FinallProj.Helpers
{
    internal static class Views
    {
        public static string Colored(string color, string text)
        {
            return $"[{color}]{Markup.Escape(text)}[/]";
        }

        public static string LectureStatus(Lecture lecture)
        {
            if (lecture.Status == Models.Enums.LectureStatus.Cancelled)
            {
                return Colored(Theme.Error, T("cancelled"));
            }

            if (lecture.Status == Models.Enums.LectureStatus.Done)
            {
                return Colored(Theme.Success, T("done"));
            }

            if (lecture.Date.Date == Clock.Today && Clock.DayOffset == 0)
            {
                TimeSpan now = DateTime.Now.TimeOfDay;

                if (now > lecture.End)
                {
                    return Colored(Theme.Muted, T("finished"));
                }

                if (now >= lecture.Start)
                {
                    return Colored(Theme.Warn, T("in progress"));
                }
            }

            if (lecture.Date.Date < Clock.Today)
            {
                return Colored(Theme.Warn, T("not marked"));
            }

            return Colored(Theme.Accent, T("planned"));
        }

        public static string Where(Lecture lecture)
        {
            string label = lecture.Format == LectureFormat.Online ? T("Online") : T("Room");

            return $"{Markup.Escape(label)}: {Markup.Escape(lecture.Place)}";
        }

        public static void ShowLectures(string title, List<Lecture> lectures, bool showDate, bool showGroup)
        {
            Ui.Title(title);

            Table table = new Table().Border(TableBorder.Rounded);

            if (showDate)
            {
                table.AddColumn(T("Date"));
            }

            table.AddColumn(T("Time"));
            table.AddColumn(T("Subject"));

            if (showGroup)
            {
                table.AddColumn(T("Group"));
            }

            table.AddColumn(T("Lecturer"));
            table.AddColumn(T("Where"));
            table.AddColumn(T("Status"));

            foreach (Lecture lecture in lectures)
            {
                List<string> cells = new List<string>();

                if (showDate)
                {
                    cells.Add(Markup.Escape(Format.Date(lecture.Date)));
                }

                cells.Add($"{lecture.StartTime}-{lecture.EndTime}");
                cells.Add(Markup.Escape(lecture.Title));

                if (showGroup)
                {
                    cells.Add(Markup.Escape(Access.GroupName(lecture.GroupId)));
                }

                cells.Add(Markup.Escape(Access.LecturerName(lecture.LecturerUsername)));
                cells.Add(Where(lecture));
                cells.Add(LectureStatus(lecture));

                table.AddRow(cells.ToArray());
            }

            Ui.Write(table);
        }

        public static string LectureLabel(Lecture lecture)
        {
            return $"{Format.Date(lecture.Date)} {lecture.StartTime}  {lecture.Title}  ({Access.GroupName(lecture.GroupId)})";
        }

        public static string ExamKind(Exam exam)
        {
            if (exam.RetakeOf != 0)
            {
                return exam.IsFinal ? T("Final retake") : T("Retake");
            }

            return exam.IsFinal ? T("Final exam") : T("Exam");
        }

        public static string ExamLabel(Exam exam)
        {
            return $"{Format.Date(exam.Date)} {exam.StartTime}  {exam.Title}  [{ExamKind(exam)}]  ({Access.GroupName(exam.GroupId)})";
        }

        public static string FinalStatusText(FinalStatus status)
        {
            return status switch
            {
                FinalStatus.Passed => Colored(Theme.Success, T("final passed")),
                FinalStatus.Pending => Colored(Theme.Warn, T("final not taken yet")),
                FinalStatus.Stuck => Colored(Theme.Error, T("STUCK IN FINAL (retake needed)")),
                FinalStatus.MustRepeat => Colored(Theme.Error, T("failed all attempts, repeat the semester")),
                _ => Colored(Theme.Muted, "-")
            };
        }

        public static string OutcomeText(YearOutcome outcome)
        {
            return outcome switch
            {
                YearOutcome.Promoted => Colored(Theme.Success, T("Promoted to the next year")),
                YearOutcome.StuckInFinal => Colored(Theme.Error, T("Stuck in final")),
                YearOutcome.LowGpa => Colored(Theme.Error, T("GPA below the pass level")),
                YearOutcome.MustRepeat => Colored(Theme.Error, T("Must repeat")),
                _ => Colored(Theme.Warn, T("In progress"))
            };
        }

        public static string GpaText(double gpa)
        {
            string color = gpa >= AcademicManagerPass ? Theme.Success : Theme.Error;

            return $"[{color}]{gpa:F2}[/]";
        }

        public const double AcademicManagerPass = Managers.AcademicManager.PassGpa;
    }
}
