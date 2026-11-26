using FinallProj.Exceptions;
using FinallProj.Helpers;
using FinallProj.Managers;
using FinallProj.Models;
using FinallProj.Models.Enums;
using Spectre.Console;
using static FinallProj.Helpers.Translator;

namespace FinallProj.Menus
{
    internal static class StudentMenu
    {
        public static void Show(Student student)
        {
            ShowDebtWarning(student);
            ShowDigest(student);

            bool loggedIn = true;

            do
            {
                List<string> labels = new List<string> { "My profile", "My payments", "Payment calendar", "My report card" };

                if (student.GroupId == 0)
                {
                    labels.Add("Enroll in a group");
                }

                labels.Add("My schedule");
                labels.Add("University news");
                labels.Add("My assignments");
                labels.Add("Exams");
                labels.Add("Points and rating");
                labels.Add("Semesters and GPA");
                labels.Add("My account");
                labels.Add("Log out");

                int choice = Ui.ChooseIndex("Student menu", labels, true, true, false, false);
                string selected = labels[choice];

                try
                {
                    switch (selected)
                    {
                        case "My profile":
                            ShowProfile(student);
                            break;
                        case "My payments":
                            ShowPayments(student);
                            break;
                        case "Payment calendar":
                            Screens.ShowPaymentCalendar(App.PaymentManager.GetPaymentsOf(student.Id));
                            break;
                        case "My report card":
                            ShowReportCard(student);
                            break;
                        case "Enroll in a group":
                            EnrollmentMenu.Enroll(student);
                            break;
                        case "My schedule":
                            ScheduleMenu.ShowStudent(student);
                            break;
                        case "University news":
                            NewsMenu.Show(student);
                            break;
                        case "My assignments":
                            AssignmentMenu.ShowStudent(student);
                            break;
                        case "Exams":
                            ExamMenu.ShowStudent(student);
                            break;
                        case "Points and rating":
                            ScoreMenu.ShowStudent(student);
                            break;
                        case "Semesters and GPA":
                            GpaMenu.ShowStudent(student);
                            break;
                        case "My account":
                            AccountMenu.Show(student);
                            break;
                        case "Log out":
                            loggedIn = false;
                            App.Log($"logout: {student.Username}");
                            Ui.ShowWarning(T("You have logged out."));
                            break;
                    }
                }
                catch (AppException ex)
                {
                    Ui.ShowError(ex.Message);
                }

                Ui.EndTask();
            } while (loggedIn);
        }

        private static void ShowDigest(Student student)
        {
            if (student.GroupId == 0)
            {
                return;
            }

            List<Lecture> today = App.LectureManager.GetForGroups(new List<int> { student.GroupId }, Clock.Today, Clock.Today);

            if (today.Count > 0)
            {
                Views.ShowLectures(T("Today's lectures") + " - " + Format.Date(Clock.Today), today, false, false);
            }

            Exam? soon = App.ExamManager.GetForGroups(new List<int> { student.GroupId }, Clock.Today).FirstOrDefault(exam => (exam.Date.Date - Clock.Today).Days <= 3);

            if (soon != null)
            {
                Ui.ShowWarning(T("Exam soon: {0}, {1} at {2}", soon.Title, Format.Date(soon.Date), soon.StartTime));
            }

            foreach (Semester semester in App.AcademicManager.GetSemesters(student))
            {
                FinalStatus status = App.ExamManager.FinalStatusOf(student, semester);

                if (status == FinalStatus.Stuck)
                {
                    Ui.ShowError(T("You are STUCK IN THE FINAL of {0}: you failed it and need a retake!", semester.Label));
                }
                else if (status == FinalStatus.MustRepeat)
                {
                    Ui.ShowError(T("You failed the final of {0} in all attempts: the semester must be repeated!", semester.Label));
                }
            }
        }

        private static void ShowDebtWarning(Student student)
        {
            if (App.PaymentManager.HasOverdue(student.Id, Clock.Today))
            {
                decimal debt = App.PaymentManager.GetOutstanding(student.Id, Clock.Today);
                Ui.ShowWarning(T("You have overdue payments! Total to pay now: {0}", Format.Money(debt)));
            }
        }

        private static void ShowProfile(Student student)
        {
            Course? course = App.CourseManager.GetCourseOfStudent(student);
            Group? group = App.CourseManager.GetGroupOfStudent(student);

            string courseText = course == null ? T("not enrolled") : course.ToString();
            string groupText = "-";

            if (group != null)
            {
                int count = App.CourseManager.GetStudentCount(group.Id, App.StudentManager.GetAllStudents());
                groupText = $"{group.Name} ({T("{0} students", count)})";
            }

            string grade = student.IsGraded() ? student.Grade.ToString() : T("not graded yet");

            string text = $"{T("Name")}: {student.Name}\n" +
                $"{T("Username")}: {student.Username}\n" +
                $"{T("Roll N")}: {student.RollNumber}\n" +
                $"{T("Course")}: {courseText}\n" +
                $"{T("Group")}: {groupText}\n" +
                $"{T("Grade")}: {grade}\n" +
                $"{T("Scholarship")}: {student.ScholarshipPercent}%";

            Panel panel = new Panel(Markup.Escape(text)).Header($"[{Theme.Title}]{Markup.Escape(T("My profile"))}[/]").Border(BoxBorder.Rounded);
            Ui.Write(panel);
        }

        private static void ShowPayments(Student student)
        {
            if (student.GroupId == 0)
            {
                Ui.ShowWarning(T("You are not enrolled in a group yet. Choose 'Enroll in a group' first."));
                return;
            }

            bool again = true;

            while (again)
            {
                List<Payment> payments = App.PaymentManager.GetPaymentsOf(student.Id);
                Tables.ShowPayments("My payments", payments);
                Screens.ShowPaymentProgress(payments);

                decimal outstanding = App.PaymentManager.GetOutstanding(student.Id, Clock.Today);
                Ui.Line($"{Markup.Escape(T("Left to pay"))}: [{Theme.Title}]{Format.Money(outstanding)}[/]");

                Payment? next = App.PaymentManager.GetNextUnpaid(student.Id);

                if (next == null)
                {
                    Ui.ShowSuccess(T("Everything is paid. Thank you!"));
                    return;
                }

                decimal toPay = next.Amount;

                if (next.GetStatus(Clock.Today) == PaymentStatus.Overdue)
                {
                    toPay += PaymentManager.LateFee;
                }

                string title = next.Number == 0 ? T("Enrollment fee") : T("Installment {0}", next.Number);

                if (!Ui.Ask(T("Pay now: {0} - {1}?", title, Format.Money(toPay)), false))
                {
                    again = false;
                }
                else
                {
                    PayNext(student, next);
                }
            }
        }

        private static void PayNext(Student student, Payment next)
        {
            List<string> methods = new List<string> { "Card", "Cash", "BankTransfer" };
            int choice = Ui.ChooseIndex("Choose a payment method", methods);

            PaymentMethod method = PaymentMethod.Card;

            if (choice == 1)
            {
                method = PaymentMethod.Cash;
            }
            else if (choice == 2)
            {
                method = PaymentMethod.BankTransfer;
            }

            Payment paid = App.PaymentManager.Pay(next.Id, student, method, Clock.Today);

            Course? course = App.CourseManager.GetCourseOfStudent(student);
            string courseName = course == null ? "-" : course.ToString();
            string path = App.Files.CreateReceipt(paid, student, courseName);

            App.SaveAll();
            App.Log($"payment: {student.Username} #{paid.Id} {Format.Money(paid.Amount + paid.LateFee)}");

            Ui.ShowSuccess(T("Payment accepted! Receipt: {0}", path));
        }

        private static void ShowReportCard(Student student)
        {
            if (App.PaymentManager.HasOverdue(student.Id, Clock.Today))
            {
                Ui.ShowWarning(T("Your report card is on hold until the overdue payments are paid."));
                return;
            }

            try
            {
                Ui.ShowReport(App.Files.ReadReport(student.RollNumber));
            }
            catch (NotFoundException)
            {
                Ui.ShowWarning(T("Your report card is not created yet, ask your lecturer."));
            }
        }
    }
}
