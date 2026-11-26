using FinallProj.Exceptions;
using FinallProj.Helpers;
using FinallProj.Models;
using FinallProj.Models.Enums;
using static FinallProj.Helpers.Translator;

namespace FinallProj.Menus
{
    internal static class AcademicSeed
    {
        public static int Load()
        {
            if (App.Lectures.Count > 0 || App.News.Count > 0 || App.Exams.Count > 0 || App.Assignments.Count > 0)
                throw new ValidationException(T("Academic starter data is already loaded."));

            List<Group> groups = App.CourseManager.GetAllGroups()
                .Where(group => group.StartDate.Date <= Clock.Today && StudentsOf(group).Count > 0)
                .OrderBy(group => group.Id)
                .ToList();

            if (groups.Count == 0)
                throw new NotFoundException(T("Load the starter courses, groups and students first!"));

            DateTime today = Clock.Today;
            User admin = App.Users.FirstOrDefault(user => user.Role == UserRole.Admin) ?? new Admin(0, "System", "system");
            Lecturer? lecturer = App.UserManager.GetAllLecturers().FirstOrDefault();
            int lectures = 0;

            for (int i = 0; i < groups.Count; i++)
            {
                Group group = groups[i];
                Course course = App.CourseManager.GetAllCourses().First(item => item.Id == group.CourseId);
                string teacher = course.LecturerUsername != string.Empty ? course.LecturerUsername : lecturer == null ? string.Empty : lecturer.Username;
                string time = TimeText.Format(TimeSpan.FromHours(10 + 2 * i));
                string room = $"A-{101 + i}";

                App.Lectures.Add(new Lecture
                {
                    Id = NextLectureId(),
                    GroupId = group.Id,
                    LecturerUsername = teacher,
                    Date = today.AddDays(-1),
                    StartTime = time,
                    Minutes = 90,
                    Title = $"{course.Name}: {T("previous lesson")}",
                    Format = LectureFormat.Room,
                    Place = room,
                    Status = LectureStatus.Done
                });

                App.LectureManager.Add(admin, group.Id, teacher, today, time, 90, $"{course.Name}: {T("today's lesson")}", LectureFormat.Room, room);
                App.LectureManager.Add(admin, group.Id, teacher, today.AddDays(1), TimeText.Format(TimeSpan.FromHours(15 + 2 * i)), 90, $"{course.Name}: {T("practice")}", LectureFormat.Room, room);
                App.LectureManager.Add(admin, group.Id, teacher, today.AddDays(3), time, 60, $"{course.Name}: {T("online consultation")}", LectureFormat.Online, "https://meet.example.com/step-" + group.Name.ToLowerInvariant());
                App.LectureManager.Add(admin, group.Id, teacher, today.AddDays(7), time, 90, $"{course.Name}: {T("new topic")}", LectureFormat.Room, room);
                lectures += 5;
            }

            SeedPoints(admin, groups, today);
            SeedAssignments(admin, lecturer, groups[0], today);
            SeedExams(admin, groups, today);
            SeedNews(admin, lecturer, today);

            App.SaveAll();
            App.Log("academic starter data loaded");

            return lectures;
        }

        private static List<Student> StudentsOf(Group group)
        {
            return App.StudentManager.GetAllStudents().Where(student => student.GroupId == group.Id).OrderBy(student => student.RollNumber).ToList();
        }

        private static int NextLectureId()
        {
            return App.Lectures.Count == 0 ? 1 : App.Lectures.Max(item => item.Id) + 1;
        }

        private static void SeedPoints(User admin, List<Group> groups, DateTime today)
        {
            int[][] patterns =
            {
                new[] { 12, 8, 15, 10, 9 },
                new[] { 6, 5, 4, 7, 5 },
                new[] { 3, 2, 8, 4, 2 }
            };

            string[] reasons = { "Active work at the lesson", "Good answer", "Homework", "Team work", "Quiz" };

            for (int g = 0; g < groups.Count; g++)
            {
                List<Student> students = StudentsOf(groups[g]);

                for (int s = 0; s < students.Count; s++)
                {
                    int[] pattern = patterns[(s + g) % patterns.Length];

                    for (int k = 0; k < pattern.Length; k++)
                    {
                        int points = pattern[(k + s) % pattern.Length] + 3 * (students.Count - s);
                        App.AcademicManager.AddScore(admin, students[s], points, T(reasons[k]), today.AddDays(-3 - 6 * k));
                    }
                }
            }
        }

        private static void SeedAssignments(User admin, Lecturer? lecturer, Group group, DateTime today)
        {
            List<Student> students = StudentsOf(group);

            App.Assignments.Add(new Assignment
            {
                Id = 1,
                GroupId = group.Id,
                LecturerUsername = lecturer == null ? admin.Username : lecturer.Username,
                Title = T("Console calculator"),
                Description = T("Write a console calculator with +, -, *, / and input checking."),
                Created = today.AddDays(-10),
                Deadline = today.AddDays(-3),
                MaxPoints = 10
            });

            Assignment first = App.AssignmentManager.Find(1);
            Assignment second = App.AssignmentManager.Add(admin, group.Id, T("Student list project"), T("Create a program with a list of students, a menu and file saving."), today.AddDays(5), 20);

            for (int i = 0; i < Math.Min(2, students.Count); i++)
            {
                Submission sent = App.AssignmentManager.Submit(students[i], first, i == 0 ? SubmissionKind.Text : SubmissionKind.Link, i == 0 ? T("My calculator uses a switch and TryParse.") : "https://github.com/example/calculator");
                sent.Date = today.AddDays(-4);
                sent.IsLate = false;
                App.AssignmentManager.Review(admin, sent, i == 0 ? 9 : 7, i == 0 ? T("Clean code, add a division by zero check.") : T("Works, but split it into methods."));
            }

            if (students.Count > 2)
            {
                Submission sent = App.AssignmentManager.Submit(students[2], first, SubmissionKind.Text, T("Sorry, I finished it a bit later."));
                sent.Date = today.AddDays(-1);
            }

            if (students.Count > 0)
            {
                App.AssignmentManager.Submit(students[0], second, SubmissionKind.Link, "https://github.com/example/student-list");
            }
        }

        private static void SeedExams(User admin, List<Group> groups, DateTime today)
        {
            Group first = groups[0];
            List<Student> firstStudents = StudentsOf(first);

            Exam final = new Exam
            {
                Id = 1,
                GroupId = first.Id,
                Title = T("Final exam"),
                Date = today.AddDays(-1),
                StartTime = "10:00",
                Place = "A-101",
                Details = T("All topics of the semester, written test and a small program."),
                IsFinal = true,
                MaxPoints = 100,
                CreatedBy = admin.Username
            };

            App.Exams.Add(final);

            int[] finalPoints = { 82, 35, -1 };

            for (int i = 0; i < firstStudents.Count && i < finalPoints.Length; i++)
            {
                if (finalPoints[i] >= 0)
                {
                    App.ExamManager.RecordResult(admin, final, firstStudents[i], finalPoints[i]);
                }
            }

            if (groups.Count > 1)
            {
                Group second = groups[1];
                List<Student> secondStudents = StudentsOf(second);

                Exam midterm = new Exam
                {
                    Id = 2,
                    GroupId = second.Id,
                    Title = T("Midterm test"),
                    Date = today.AddDays(-1),
                    StartTime = "12:00",
                    Place = "A-102",
                    Details = T("Topics of the first half of the course."),
                    MaxPoints = 50,
                    CreatedBy = admin.Username
                };

                App.Exams.Add(midterm);

                int[] midtermPoints = { 44, 17 };

                for (int i = 0; i < secondStudents.Count && i < midtermPoints.Length; i++)
                {
                    App.ExamManager.RecordResult(admin, midterm, secondStudents[i], midtermPoints[i]);
                }

                App.ExamManager.AddRetake(admin, midterm.Id, today.AddDays(3), "12:00", "A-102", T("Retake for students who failed."));
            }

            if (groups.Count > 2)
            {
                App.ExamManager.Add(admin, groups[2].Id, T("Speaking test"), today.AddDays(5), "14:00", "A-103", T("Prepare a 3 minute talk about your city."), false, 40);
            }

            App.ExamManager.Add(admin, first.Id, T("Practical exam"), today.AddDays(2), "10:00", "A-101", T("Solve 3 tasks on the computer."), false, 60);
        }

        private static void SeedNews(User admin, Lecturer? lecturer, DateTime today)
        {
            App.NewsManager.Add(admin, T("Welcome to the new semester"), T("Classes are running as planned. Check your schedule in the program every morning."), "General");
            App.NewsManager.Add(admin, T("Exam period"), T("The exam schedule is published. Final exams are announced in the Exams section."), "Exams");

            if (lecturer != null)
            {
                App.NewsManager.Add(lecturer, T("Open consultation"), T("I will hold an open online consultation this week. Bring your questions."), "Schedule");
            }
        }
    }
}
