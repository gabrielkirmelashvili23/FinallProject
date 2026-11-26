using FinallProj.Exceptions;
using FinallProj.Helpers;
using FinallProj.Models;
using FinallProj.Models.Enums;
using Spectre.Console;
using static FinallProj.Helpers.Translator;

namespace FinallProj.Menus
{
    internal static class ScheduleMenu
    {
        public static void ShowStudent(Student student)
        {
            if (student.GroupId == 0)
            {
                Ui.ShowWarning(T("You are not enrolled in a group yet. Choose 'Enroll in a group' first."));
                return;
            }

            ShowToday(student);

            List<MenuItem> items = new List<MenuItem>
            {
                new MenuItem("Today's lectures", () => ShowToday(student)),
                new MenuItem("Upcoming lectures (next 14 days)", () => ShowUpcoming(student, 14)),
                new MenuItem("Upcoming lectures (next 30 days)", () => ShowUpcoming(student, 30))
            };

            Ui.RunMenu("My schedule", items);
        }

        private static void ShowToday(User user)
        {
            List<int> groupIds = Access.GroupsFor(user).Select(group => group.Id).ToList();
            List<Lecture> today = App.LectureManager.GetForGroups(groupIds, Clock.Today, Clock.Today);

            if (today.Count == 0)
            {
                Ui.ShowInfo(T("No lectures today ({0}).", Format.Date(Clock.Today)));
                return;
            }

            Views.ShowLectures(T("Today's lectures") + " - " + Format.Date(Clock.Today), today, false, user.Role != UserRole.Student);
        }

        private static void ShowUpcoming(User user, int days)
        {
            List<int> groupIds = Access.GroupsFor(user).Select(group => group.Id).ToList();
            List<Lecture> upcoming = App.LectureManager.GetForGroups(groupIds, Clock.Today, Clock.Today.AddDays(days));

            if (user.Role == UserRole.Lecturer)
            {
                upcoming = upcoming.Where(lecture => lecture.LecturerUsername == user.Username || groupIds.Contains(lecture.GroupId)).ToList();
            }

            if (upcoming.Count == 0)
            {
                Ui.ShowInfo(T("No lectures planned for the next {0} days.", days));
                return;
            }

            Views.ShowLectures(T("Upcoming lectures (next {0} days)", days), upcoming, true, user.Role != UserRole.Student);
        }

        public static void ShowStaff(User user)
        {
            List<MenuItem> items = new List<MenuItem>
            {
                new MenuItem("Today's lectures", () => ShowToday(user)),
                new MenuItem("Upcoming lectures (next 14 days)", () => ShowUpcoming(user, 14)),
                new MenuItem("Upcoming lectures (next 30 days)", () => ShowUpcoming(user, 30)),
                new MenuItem("Plan a lecture", () => PlanLecture(user)),
                new MenuItem("Reschedule a lecture", () => Reschedule(user)),
                new MenuItem("Mark a lecture done or cancelled", () => SetStatus(user)),
                new MenuItem("Delete a lecture", () => Delete(user))
            };

            Ui.RunMenu("Lectures", items);
        }

        private static void PlanLecture(User user)
        {
            Group group = Pickers.PickGroup(user);
            string lecturer = Pickers.PickLecturer(user, group);

            string title = Ui.ReadText("Enter the subject of the lecture:", 3);
            int formatChoice = Ui.ChooseIndex("Choose the format", new List<string> { "Room", "Online" });
            LectureFormat format = formatChoice == 0 ? LectureFormat.Room : LectureFormat.Online;
            string place = Ui.ReadText(format == LectureFormat.Room ? "Enter the room:" : "Enter the link (https://...):", 2);

            DateTime date = Ui.ReadDate("Enter the date (yyyy-MM-dd):", Clock.Today);
            string time = Ui.ReadTime("Enter the start time (HH:mm):", "09:00");
            int minutes = Ui.ReadNumberInRange("Enter the duration in minutes:", 15, 300);

            int repeats = 1;

            if (Ui.Ask(T("Repeat this lecture every week?"), false))
            {
                repeats = Ui.ReadNumberInRange("For how many weeks in total?", 2, 20);
            }

            int added = 0;

            try
            {
                for (int i = 0; i < repeats; i++)
                {
                    App.LectureManager.Add(user, group.Id, lecturer, date.AddDays(7 * i), time, minutes, title, format, place);
                    added++;
                }
            }
            finally
            {
                if (added > 0)
                {
                    App.SaveAll();
                    App.Log($"lectures planned: {title} x{added} for {group.Name}");
                    Ui.ShowSuccess(T("{0} lecture(s) planned!", added));
                }
            }
        }

        private static Lecture ChooseLecture(User user, bool includePast)
        {
            DateTime from = includePast ? Clock.Today.AddDays(-60) : Clock.Today;
            List<int> groupIds = Access.GroupsFor(user).Select(group => group.Id).ToList();

            List<Lecture> lectures = App.LectureManager.GetAll()
                .Where(lecture => lecture.Date.Date >= from && (user.Role == UserRole.Admin || groupIds.Contains(lecture.GroupId) || lecture.LecturerUsername == user.Username))
                .Take(60)
                .ToList();

            int index = Pickers.PickIndex("Choose a lecture", lectures, Views.LectureLabel);

            return lectures[index];
        }

        private static void Reschedule(User user)
        {
            Lecture lecture = ChooseLecture(user, false);

            DateTime date = Ui.ReadDate("Enter the new date (yyyy-MM-dd):", lecture.Date);
            string time = Ui.ReadTime("Enter the new start time (HH:mm):", lecture.StartTime);
            int minutes = Ui.ReadNumberInRange("Enter the duration in minutes:", 15, 300);
            string place = lecture.Place;

            if (Ui.Ask(T("Change the {0} too?", lecture.Format == LectureFormat.Room ? T("room") : T("link")), false))
            {
                place = Ui.ReadText(lecture.Format == LectureFormat.Room ? "Enter the room:" : "Enter the link (https://...):", 2);
            }

            App.LectureManager.Reschedule(user, lecture.Id, date, time, minutes, place);
            App.SaveAll();
            App.Log($"lecture rescheduled: #{lecture.Id}");

            Ui.ShowSuccess(T("Lecture moved!"));
        }

        private static void SetStatus(User user)
        {
            Lecture lecture = ChooseLecture(user, true);
            int choice = Ui.ChooseIndex("Choose the new status", new List<string> { "Planned", "Done", "Cancelled" });

            LectureStatus status = choice == 0 ? LectureStatus.Planned : choice == 1 ? LectureStatus.Done : LectureStatus.Cancelled;

            App.LectureManager.SetStatus(user, lecture.Id, status);
            App.SaveAll();
            App.Log($"lecture status: #{lecture.Id} {status}");

            Ui.ShowSuccess(T("Status updated!"));
        }

        private static void Delete(User user)
        {
            Lecture lecture = ChooseLecture(user, true);

            if (!Ui.Confirm("Really delete this lecture?"))
            {
                Ui.ShowWarning(T("Deletion cancelled."));
                return;
            }

            App.LectureManager.Delete(user, lecture.Id);
            App.SaveAll();
            App.Log($"lecture deleted: #{lecture.Id}");

            Ui.ShowSuccess(T("Lecture deleted!"));
        }
    }
}
