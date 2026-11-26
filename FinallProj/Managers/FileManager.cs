using FinallProj.Exceptions;
using FinallProj.Helpers;
using FinallProj.Models;
using FinallProj.Models.Enums;
using static FinallProj.Helpers.Translator;

namespace FinallProj.Managers
{
    internal class FileManager
    {
        private string _dataFolder;

        public string DataFolder
        {
            get
            {
                return _dataFolder;
            }
        }

        private string _usersFilePath;
        private string _coursesFilePath;
        private string _groupsFilePath;
        private string _paymentsFilePath;
        private string _settingsFilePath;
        private string _logFilePath;
        private string _reportsFolder;
        private string _receiptsFolder;

        public FileManager()
        {
            _dataFolder = "Data";
            _usersFilePath = _dataFolder + "/users.txt";
            _coursesFilePath = _dataFolder + "/courses.txt";
            _groupsFilePath = _dataFolder + "/groups.txt";
            _paymentsFilePath = _dataFolder + "/payments.txt";
            _settingsFilePath = _dataFolder + "/settings.txt";
            _logFilePath = _dataFolder + "/audit.log";
            _reportsFolder = _dataFolder + "/Reports";
            _receiptsFolder = _dataFolder + "/Receipts";
        }

        public FileManager(string dataFolder)
        {
            _dataFolder = dataFolder;
            _usersFilePath = _dataFolder + "/users.txt";
            _coursesFilePath = _dataFolder + "/courses.txt";
            _groupsFilePath = _dataFolder + "/groups.txt";
            _paymentsFilePath = _dataFolder + "/payments.txt";
            _settingsFilePath = _dataFolder + "/settings.txt";
            _logFilePath = _dataFolder + "/audit.log";
            _reportsFolder = _dataFolder + "/Reports";
            _receiptsFolder = _dataFolder + "/Receipts";
        }

        private void EnsureFolder(string folder)
        {
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }
        }

        public List<User> LoadUsers()
        {
            EnsureFolder(_dataFolder);

            List<User> users = new List<User>();

            if (!File.Exists(_usersFilePath))
            {
                return users;
            }

            string[] lines = File.ReadAllLines(_usersFilePath);

            for (int i = 0; i < lines.Length; i++)
            {
                if (!string.IsNullOrWhiteSpace(lines[i]))
                {
                    try
                    {
                        users.Add(ParseUser(lines[i]));
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(T("users file: line {0} is damaged and skipped ({1})", i + 1, ex.Message));
                    }
                }
            }

            return users;
        }

        public void SaveUsers(List<User> users)
        {
            EnsureFolder(_dataFolder);

            List<string> lines = users
                .Select(user => user.ToFileLine())
                .ToList();

            File.WriteAllLines(_usersFilePath, lines);
        }

        private User ParseUser(string line)
        {
            string[] parts = line.Split('|');

            string role = parts[0];
            int id = int.Parse(parts[1]);
            string name = parts[2];
            string username = parts[3];

            User user;

            switch (role)
            {
                case "Admin":
                    user = new Admin(id, name, username);
                    break;
                case "Lecturer":
                    user = new Lecturer(id, name, username, parts[7]);
                    break;
                case "Student":
                    int rollNumber = int.Parse(parts[7]);
                    char grade = char.Parse(parts[8]);
                    int groupId = int.Parse(parts[9]);
                    int scholarship = int.Parse(parts[10]);
                    user = new Student(id, name, username, rollNumber, grade, groupId, scholarship);
                    break;
                default:
                    throw new FormatException($"Unknown role: {role}");
            }

            user.LoadSecrets(parts[4], parts[5], parts[6]);

            return user;
        }

        public List<Course> LoadCourses()
        {
            List<Course> courses = new List<Course>();

            foreach (string line in ReadLines(_coursesFilePath))
            {
                string[] parts = line.Split('|');
                courses.Add(new Course(int.Parse(parts[0]), parts[1], parts[2], Format.ParseNumber(parts[3]), int.Parse(parts[4]), parts[5]));
            }

            return courses;
        }

        public void SaveCourses(List<Course> courses)
        {
            EnsureFolder(_dataFolder);
            File.WriteAllLines(_coursesFilePath, courses.Select(course => course.ToFileLine()).ToList());
        }

        public List<Group> LoadGroups()
        {
            List<Group> groups = new List<Group>();

            foreach (string line in ReadLines(_groupsFilePath))
            {
                string[] parts = line.Split('|');
                groups.Add(new Group(int.Parse(parts[0]), parts[1], int.Parse(parts[2]), int.Parse(parts[3]), Format.ParseDate(parts[4])));
            }

            return groups;
        }

        public void SaveGroups(List<Group> groups)
        {
            EnsureFolder(_dataFolder);
            File.WriteAllLines(_groupsFilePath, groups.Select(group => group.ToFileLine()).ToList());
        }

        public List<Payment> LoadPayments()
        {
            List<Payment> payments = new List<Payment>();

            foreach (string line in ReadLines(_paymentsFilePath))
            {
                string[] parts = line.Split('|');

                Payment payment = new Payment(int.Parse(parts[0]), int.Parse(parts[1]), int.Parse(parts[2]), Format.ParseNumber(parts[3]), Format.ParseDate(parts[4]));
                payment.IsPaid = bool.Parse(parts[5]);

                if (payment.IsPaid)
                {
                    payment.PaidDate = Format.ParseDate(parts[6]);
                }

                payment.LateFee = Format.ParseNumber(parts[7]);
                payment.Method = Enum.Parse<PaymentMethod>(parts[8]);

                payments.Add(payment);
            }

            return payments;
        }

        public void SavePayments(List<Payment> payments)
        {
            EnsureFolder(_dataFolder);
            File.WriteAllLines(_paymentsFilePath, payments.Select(payment => payment.ToFileLine()).ToList());
        }

        private List<string> ReadLines(string path)
        {
            List<string> lines = new List<string>();

            if (!File.Exists(path))
            {
                return lines;
            }

            foreach (string line in File.ReadAllLines(path))
            {
                if (!string.IsNullOrWhiteSpace(line))
                {
                    lines.Add(line);
                }
            }

            return lines;
        }

        public string[] LoadSettings()
        {
            string[] defaults = { "en", "0", UserManager.LecturerCode, UserManager.AdminCode, "chocolate", "1" };

            if (!File.Exists(_settingsFilePath))
            {
                return defaults;
            }

            string[] parts = File.ReadAllText(_settingsFilePath).Trim().Split('|');

            if (parts.Length == 4)
            {
                return new[] { parts[0], parts[1], parts[2], parts[3], "chocolate", "1" };
            }

            if (parts.Length == 5)
            {
                return new[] { parts[0], parts[1], parts[2], parts[3], parts[4], "1" };
            }

            if (parts.Length != 6)
            {
                return defaults;
            }

            return parts;
        }

        public void SaveSettings(string language, int dayOffset, string lecturerCode, string adminCode, string background = "chocolate", bool icons = true)
        {
            EnsureFolder(_dataFolder);
            File.WriteAllText(_settingsFilePath, $"{language}|{dayOffset}|{lecturerCode}|{adminCode}|{background}|{(icons ? "1" : "0")}");
        }

        public void AppendLog(string text)
        {
            EnsureFolder(_dataFolder);
            File.AppendAllText(_logFilePath, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} | {text}{Environment.NewLine}");
        }

        public List<string> ReadLog(int count)
        {
            if (!File.Exists(_logFilePath))
            {
                return new List<string>();
            }

            return File.ReadAllLines(_logFilePath).TakeLast(count).ToList();
        }

        private string GetReportPath(int rollNumber)
        {
            return _reportsFolder + $"/Student_{rollNumber}.txt";
        }

        public string CreateReport(Student student, string courseName, string groupName, decimal outstanding)
        {
            EnsureFolder(_reportsFolder);

            string status = T("not graded yet");

            if (student.IsGraded())
            {
                status = student.HasPassed() ? T("passed") : T("failed");
            }

            string grade = student.IsGraded() ? $"{student.Grade} ({student.GetGradePoints()} / 5)" : "-";

            string content = $"--- {T("STUDENT REPORT CARD")} ---\n" +
                $"{T("Name")}: {student.Name}\n" +
                $"{T("Roll N")}: {student.RollNumber}\n" +
                $"{T("Course")}: {courseName}\n" +
                $"{T("Group")}: {groupName}\n" +
                $"{T("Grade")}: {grade}\n" +
                $"{T("Status")}: {status}\n" +
                $"{T("Unpaid balance")}: {Format.Money(outstanding)}";

            string path = GetReportPath(student.RollNumber);
            File.WriteAllText(path, content);

            return path;
        }

        public bool ReportExists(int rollNumber)
        {
            return File.Exists(GetReportPath(rollNumber));
        }

        public string ReadReport(int rollNumber)
        {
            if (!ReportExists(rollNumber))
                throw new NotFoundException(T("Report card for roll number {0} not found!", rollNumber));

            return File.ReadAllText(GetReportPath(rollNumber));
        }

        public bool DeleteReport(int rollNumber)
        {
            if (!ReportExists(rollNumber))
            {
                return false;
            }

            File.Delete(GetReportPath(rollNumber));
            return true;
        }

        public string CreateReceipt(Payment payment, Student student, string courseName)
        {
            EnsureFolder(_receiptsFolder);

            string title = payment.Number == 0 ? T("Enrollment fee") : T("Installment {0}", payment.Number);

            string content = $"--- {T("PAYMENT RECEIPT")} #{payment.Id} ---\n" +
                $"{T("Name")}: {student.Name} (N{student.RollNumber})\n" +
                $"{T("Course")}: {courseName}\n" +
                $"{T("Payment")}: {title}\n" +
                $"{T("Amount")}: {Format.Money(payment.Amount)}\n" +
                $"{T("Late fee")}: {Format.Money(payment.LateFee)}\n" +
                $"{T("Total paid")}: {Format.Money(payment.Amount + payment.LateFee)}\n" +
                $"{T("Method")}: {T(payment.Method.ToString())}\n" +
                $"{T("Date")}: {Format.Date(payment.PaidDate)}";

            string path = _receiptsFolder + $"/Receipt_{payment.Id}.txt";
            File.WriteAllText(path, content);

            return path;
        }

        public string ExportPaymentsCsv(List<string> rows)
        {
            EnsureFolder(_dataFolder);

            List<string> lines = new List<string> { "roll,name,course,payment,amount,due_date,status,paid_date,late_fee" };
            lines.AddRange(rows);

            string path = _dataFolder + "/payments_export.csv";
            File.WriteAllLines(path, lines);

            return path;
        }
    }
}
