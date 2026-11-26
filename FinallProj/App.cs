using System.Text;
using FinallProj.Helpers;
using FinallProj.Managers;
using FinallProj.Models;

namespace FinallProj
{
    internal static class App
    {
        public static FileManager Files { get; private set; } = new FileManager();

        public static List<User> Users { get; private set; } = new List<User>();
        public static List<Course> Courses { get; private set; } = new List<Course>();
        public static List<Group> Groups { get; private set; } = new List<Group>();
        public static List<Payment> Payments { get; private set; } = new List<Payment>();
        public static List<Lecture> Lectures { get; private set; } = new List<Lecture>();
        public static List<NewsItem> News { get; private set; } = new List<NewsItem>();
        public static List<ScoreEntry> Scores { get; private set; } = new List<ScoreEntry>();
        public static List<Assignment> Assignments { get; private set; } = new List<Assignment>();
        public static List<Submission> Submissions { get; private set; } = new List<Submission>();
        public static List<Exam> Exams { get; private set; } = new List<Exam>();
        public static List<ExamResult> ExamResults { get; private set; } = new List<ExamResult>();
        public static List<YearRecord> YearRecords { get; private set; } = new List<YearRecord>();

        public static UserManager UserManager { get; private set; } = null!;
        public static StudentManager StudentManager { get; private set; } = null!;
        public static CourseManager CourseManager { get; private set; } = null!;
        public static PaymentManager PaymentManager { get; private set; } = null!;
        public static LectureManager LectureManager { get; private set; } = null!;
        public static NewsManager NewsManager { get; private set; } = null!;
        public static ExamManager ExamManager { get; private set; } = null!;
        public static AcademicManager AcademicManager { get; private set; } = null!;
        public static AssignmentManager AssignmentManager { get; private set; } = null!;

        public static void Start()
        {
            Console.OutputEncoding = Encoding.UTF8;
            Console.InputEncoding = Encoding.UTF8;

            string[] settings = Files.LoadSettings();

            Translator.SetLanguage(settings[0]);

            if (int.TryParse(settings[1], out int dayOffset))
            {
                Clock.DayOffset = dayOffset;
            }

            UserManager.LecturerCode = settings[2];
            UserManager.AdminCode = settings[3];
            Background.Set(settings[4]);
            Icons.Enabled = settings[5] != "0";

            Users = Files.LoadUsers();
            Courses = Files.LoadCourses();
            Groups = Files.LoadGroups();
            Payments = Files.LoadPayments();
            Lectures = DataStore.Load(Path("lectures.txt"), Lecture.FromFileLine, "lectures");
            News = DataStore.Load(Path("news.txt"), NewsItem.FromFileLine, "news");
            Scores = DataStore.Load(Path("scores.txt"), ScoreEntry.FromFileLine, "scores");
            Assignments = DataStore.Load(Path("assignments.txt"), Assignment.FromFileLine, "assignments");
            Submissions = DataStore.Load(Path("submissions.txt"), Submission.FromFileLine, "submissions");
            Exams = DataStore.Load(Path("exams.txt"), Exam.FromFileLine, "exams");
            ExamResults = DataStore.Load(Path("examresults.txt"), ExamResult.FromFileLine, "exam results");
            YearRecords = DataStore.Load(Path("years.txt"), YearRecord.FromFileLine, "years");

            UserManager = new UserManager(Users);
            StudentManager = new StudentManager(Users);
            CourseManager = new CourseManager(Courses, Groups);
            PaymentManager = new PaymentManager(Payments);
            LectureManager = new LectureManager(Lectures);
            NewsManager = new NewsManager(News);
            ExamManager = new ExamManager(Exams, ExamResults);
            AcademicManager = new AcademicManager(Scores, YearRecords, ExamManager);
            AssignmentManager = new AssignmentManager(Assignments, Submissions, AcademicManager);
        }

        private static string Path(string name)
        {
            return System.IO.Path.Combine(Files.DataFolder, name);
        }

        public static void RemoveGroupData(int groupId)
        {
            LectureManager.RemoveOfGroup(groupId);
            ExamManager.RemoveOfGroup(groupId);
            AssignmentManager.RemoveOfGroup(groupId);
        }

        public static void RemoveStudentData(int studentId)
        {
            AssignmentManager.RemoveOfStudent(studentId);
            ExamManager.RemoveOfStudent(studentId);
            AcademicManager.RemoveOfStudent(studentId);
        }

        public static void SaveAll()
        {
            Files.SaveUsers(Users);
            Files.SaveCourses(Courses);
            Files.SaveGroups(Groups);
            Files.SavePayments(Payments);
            DataStore.Save(Path("lectures.txt"), Lectures, item => item.ToFileLine());
            DataStore.Save(Path("news.txt"), News, item => item.ToFileLine());
            DataStore.Save(Path("scores.txt"), Scores, item => item.ToFileLine());
            DataStore.Save(Path("assignments.txt"), Assignments, item => item.ToFileLine());
            DataStore.Save(Path("submissions.txt"), Submissions, item => item.ToFileLine());
            DataStore.Save(Path("exams.txt"), Exams, item => item.ToFileLine());
            DataStore.Save(Path("examresults.txt"), ExamResults, item => item.ToFileLine());
            DataStore.Save(Path("years.txt"), YearRecords, item => item.ToFileLine());
        }

        public static void SaveSettings()
        {
            Files.SaveSettings(Translator.Language, Clock.DayOffset, UserManager.LecturerCode, UserManager.AdminCode, Background.Current, Icons.Enabled);
        }

        public static void Log(string text)
        {
            Files.AppendLog(text);
        }
    }
}
