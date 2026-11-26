using FinallProj.Exceptions;
using FinallProj.Helpers;
using FinallProj.Managers;
using FinallProj.Models;
using FinallProj.Models.Enums;
using Spectre.Console;
using static FinallProj.Helpers.Translator;

namespace FinallProj.Menus
{
    internal static class AssignmentMenu
    {
        public static void ShowStudent(Student student)
        {
            if (student.GroupId == 0)
            {
                Ui.ShowWarning(T("You are not enrolled in a group yet. Choose 'Enroll in a group' first."));
                return;
            }

            bool back = false;

            while (!back)
            {
                List<Assignment> assignments = App.AssignmentManager.GetForGroups(new List<int> { student.GroupId });

                if (assignments.Count == 0)
                {
                    Ui.ShowInfo(T("No assignments yet."));
                    return;
                }

                ShowStudentTable(student, assignments);

                List<string> labels = assignments.Select(item => $"{Format.Date(item.Deadline)}  {item.Title}  [{StudentStatus(student, item, false)}]").ToList();
                labels.Add("Back");

                int choice = Ui.ChooseIndex("Choose an assignment", labels, false, false, false);

                if (choice == assignments.Count)
                {
                    back = true;
                    continue;
                }

                try
                {
                    OpenAssignment(student, assignments[choice]);
                }
                catch (AppException ex)
                {
                    Ui.ShowError(ex.Message);
                }
            }
        }

        private static string StudentStatus(Student student, Assignment assignment, bool markup)
        {
            Submission? submission = App.AssignmentManager.GetSubmission(assignment.Id, student.Id);
            string text;
            string color;

            if (submission == null)
            {
                bool late = Clock.Today > assignment.Deadline;
                text = late ? T("overdue, not submitted") : T("not submitted");
                color = late ? Theme.Error : Theme.Warn;
            }
            else if (submission.IsReviewed)
            {
                text = T("reviewed: {0}/{1}", submission.Score, assignment.MaxPoints);
                color = Theme.Success;
            }
            else
            {
                text = submission.IsLate ? T("submitted late, waiting for review") : T("submitted, waiting for review");
                color = Theme.Accent;
            }

            return markup ? $"[{color}]{Markup.Escape(text)}[/]" : text;
        }

        private static void ShowStudentTable(Student student, List<Assignment> assignments)
        {
            Ui.Title(T("My assignments"));

            Table table = new Table().Border(TableBorder.Rounded);
            table.AddColumn(T("Assignment"));
            table.AddColumn(T("Deadline"));
            table.AddColumn(new TableColumn(T("Max")).RightAligned());
            table.AddColumn(T("Status"));

            foreach (Assignment assignment in assignments)
            {
                table.AddRow(Markup.Escape(assignment.Title), Format.Date(assignment.Deadline), assignment.MaxPoints.ToString(), StudentStatus(student, assignment, true));
            }

            Ui.Write(table);
        }

        private static void OpenAssignment(Student student, Assignment assignment)
        {
            Submission? submission = App.AssignmentManager.GetSubmission(assignment.Id, student.Id);

            string text = $"{assignment.Description}\n\n{T("Deadline")}: {Format.Date(assignment.Deadline)}\n{T("Maximum points")}: {assignment.MaxPoints}\n{T("Lecturer")}: {Access.LecturerName(assignment.LecturerUsername)}";

            Ui.Write(new Panel(Markup.Escape(text)).Header($"[{Theme.Title}]{Markup.Escape(assignment.Title)}[/]").Border(BoxBorder.Rounded).Padding(2, 1));

            if (submission != null)
            {
                string mine = $"{T("Sent")}: {Format.Date(submission.Date)} ({KindText(submission.Kind)}){(submission.IsLate ? " - " + T("late") : string.Empty)}\n{submission.Content}";

                if (submission.IsReviewed)
                {
                    mine += $"\n\n{T("Score")}: {submission.Score}/{assignment.MaxPoints}\n{T("Comment")}: {submission.Comment}\n{T("Reviewed by")}: {Access.LecturerName(submission.ReviewedBy)} ({Format.Date(submission.ReviewedDate)})";
                }

                Ui.Write(new Panel(Markup.Escape(mine)).Header($"[{Theme.Accent}]{Markup.Escape(T("My work"))}[/]").Border(BoxBorder.Rounded).Padding(2, 1));

                if (submission.IsReviewed)
                {
                    return;
                }
            }

            string question = submission == null ? T("Submit your work now?") : T("Replace your work with a new version?");

            if (!Ui.Ask(question, submission == null))
            {
                return;
            }

            int kindChoice = Ui.ChooseIndex("How do you want to submit?", new List<string> { "Write an answer", "Send a link", "Upload a file" });
            SubmissionKind kind = kindChoice == 0 ? SubmissionKind.Text : kindChoice == 1 ? SubmissionKind.Link : SubmissionKind.File;

            string content = kind == SubmissionKind.Text
                ? Ui.ReadText("Enter your answer:", 3)
                : kind == SubmissionKind.Link
                    ? Ui.ReadText("Enter the link (https://...):", 8)
                    : Ui.ReadText("Enter the full path of the file:", 3);

            Submission sent = App.AssignmentManager.Submit(student, assignment, kind, content);
            App.SaveAll();
            App.Log($"submission: {student.Username} assignment #{assignment.Id}");

            Ui.ShowSuccess(sent.IsLate ? T("Work sent, but it is LATE.") : T("Work sent successfully!"));
        }

        private static string KindText(SubmissionKind kind)
        {
            return kind == SubmissionKind.Text ? T("answer") : kind == SubmissionKind.Link ? T("link") : T("file");
        }

        public static void ShowStaff(User user)
        {
            List<MenuItem> items = new List<MenuItem>
            {
                new MenuItem("Create an assignment", () => Create(user)),
                new MenuItem("Review submissions", () => ReviewMenu(user)),
                new MenuItem("View assignments", () => ShowAll(user)),
                new MenuItem("Delete an assignment", () => Delete(user))
            };

            Ui.RunMenu("Assignments", items);
        }

        private static void Create(User user)
        {
            Group group = Pickers.PickGroup(user);

            string title = Ui.ReadText("Enter the title of the assignment:", 3);
            string description = Ui.ReadText("Enter the description of the assignment:", 5);
            DateTime deadline = Ui.ReadDate("Enter the deadline (yyyy-MM-dd):", Clock.Today.AddDays(7));
            int max = Ui.ReadNumberInRange("Enter the maximum points:", 1, AcademicManager.MaxEntryPoints);

            Assignment assignment = App.AssignmentManager.Add(user, group.Id, title, description, deadline, max);
            App.SaveAll();
            App.Log($"assignment created: #{assignment.Id} for {group.Name}");

            Ui.ShowSuccess(T("Assignment created! Students of {0} can see it now.", group.Name));
        }

        private static List<Assignment> VisibleAssignments(User user)
        {
            List<int> groupIds = Access.GroupsFor(user).Select(group => group.Id).ToList();

            return App.AssignmentManager.GetForGroups(groupIds);
        }

        private static string AssignmentLabel(Assignment assignment)
        {
            int total = App.StudentManager.GetAllStudents().Count(student => student.GroupId == assignment.GroupId);
            List<Submission> submissions = App.AssignmentManager.GetSubmissions(assignment.Id);

            return $"{Format.Date(assignment.Deadline)}  {assignment.Title}  ({Access.GroupName(assignment.GroupId)}, {submissions.Count}/{total}, {T("to review")}: {submissions.Count(item => !item.IsReviewed)})";
        }

        private static void ShowAll(User user)
        {
            List<Assignment> assignments = VisibleAssignments(user);

            if (assignments.Count == 0)
            {
                Ui.ShowInfo(T("No assignments yet."));
                return;
            }

            Ui.Title(T("Assignments"));

            Table table = new Table().Border(TableBorder.Rounded);
            table.AddColumn(T("Assignment"));
            table.AddColumn(T("Group"));
            table.AddColumn(T("Deadline"));
            table.AddColumn(new TableColumn(T("Max")).RightAligned());
            table.AddColumn(new TableColumn(T("Submitted")).RightAligned());
            table.AddColumn(new TableColumn(T("To review")).RightAligned());

            foreach (Assignment assignment in assignments)
            {
                int total = App.StudentManager.GetAllStudents().Count(student => student.GroupId == assignment.GroupId);
                List<Submission> submissions = App.AssignmentManager.GetSubmissions(assignment.Id);

                table.AddRow(Markup.Escape(assignment.Title), Markup.Escape(Access.GroupName(assignment.GroupId)), Format.Date(assignment.Deadline), assignment.MaxPoints.ToString(), $"{submissions.Count}/{total}", submissions.Count(item => !item.IsReviewed).ToString());
            }

            Ui.Write(table);
        }

        private static void ReviewMenu(User user)
        {
            List<Assignment> assignments = VisibleAssignments(user);
            int index = Pickers.PickIndex("Choose an assignment", assignments, AssignmentLabel);
            Assignment assignment = assignments[index];

            bool back = false;

            while (!back)
            {
                List<Student> students = App.StudentManager.GetAllStudents().Where(student => student.GroupId == assignment.GroupId).OrderBy(student => student.Name).ToList();

                Table table = new Table().Border(TableBorder.Rounded).Title(Markup.Escape(assignment.Title));
                table.AddColumn(T("Student"));
                table.AddColumn(T("Sent"));
                table.AddColumn(T("Status"));

                List<Student> withWork = new List<Student>();

                foreach (Student student in students)
                {
                    Submission? submission = App.AssignmentManager.GetSubmission(assignment.Id, student.Id);

                    if (submission == null)
                    {
                        table.AddRow(Markup.Escape(student.Name), "-", Views.Colored(Theme.Muted, T("not submitted")));
                        continue;
                    }

                    withWork.Add(student);
                    string status = submission.IsReviewed
                        ? Views.Colored(Theme.Success, T("reviewed: {0}/{1}", submission.Score, assignment.MaxPoints))
                        : Views.Colored(Theme.Warn, submission.IsLate ? T("late, to review") : T("to review"));

                    table.AddRow(Markup.Escape(student.Name), Format.Date(submission.Date), status);
                }

                Ui.Write(table);

                if (withWork.Count == 0)
                {
                    return;
                }

                List<string> labels = withWork.Select(student => student.Name).ToList();
                labels.Add("Back");

                int choice = Ui.ChooseIndex("Choose a student to review", labels, false, false, false);

                if (choice == withWork.Count)
                {
                    back = true;
                    continue;
                }

                try
                {
                    Review(user, assignment, withWork[choice]);
                }
                catch (AppException ex)
                {
                    Ui.ShowError(ex.Message);
                }
            }
        }

        private static void Review(User user, Assignment assignment, Student student)
        {
            Submission submission = App.AssignmentManager.GetSubmission(assignment.Id, student.Id)!;

            string work = $"{T("Sent")}: {Format.Date(submission.Date)} ({KindText(submission.Kind)}){(submission.IsLate ? " - " + T("late") : string.Empty)}\n{submission.Content}";

            if (submission.IsReviewed)
            {
                work += $"\n\n{T("Current score")}: {submission.Score}/{assignment.MaxPoints}\n{T("Comment")}: {submission.Comment}";
            }

            Ui.Write(new Panel(Markup.Escape(work)).Header($"[{Theme.Title}]{Markup.Escape(student.Name)}[/]").Border(BoxBorder.Rounded).Padding(2, 1));

            int score = Ui.ReadNumberInRange(T("Enter the score (0-{0}):", assignment.MaxPoints), 0, assignment.MaxPoints);
            string comment = Ui.ReadText("Enter your comment for the student:", 2);

            App.AssignmentManager.Review(user, submission, score, comment);
            App.SaveAll();
            App.Log($"review: {student.Username} assignment #{assignment.Id} {score}/{assignment.MaxPoints}");

            Ui.ShowSuccess(T("Review saved. {0} points were added to the student's points.", score));
        }

        private static void Delete(User user)
        {
            List<Assignment> assignments = VisibleAssignments(user);
            int index = Pickers.PickIndex("Choose an assignment", assignments, AssignmentLabel);

            if (!Ui.Confirm("Really delete this assignment? All submissions and their points will be removed."))
            {
                Ui.ShowWarning(T("Deletion cancelled."));
                return;
            }

            App.AssignmentManager.Delete(user, assignments[index].Id);
            App.SaveAll();
            App.Log($"assignment deleted: #{assignments[index].Id}");

            Ui.ShowSuccess(T("Assignment deleted!"));
        }
    }
}
