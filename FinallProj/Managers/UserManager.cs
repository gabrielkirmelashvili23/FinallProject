using FinallProj.Exceptions;
using FinallProj.Helpers;
using FinallProj.Models;
using FinallProj.Models.Enums;
using static FinallProj.Helpers.Translator;

namespace FinallProj.Managers
{
    internal class UserManager
    {
        public static string LecturerCode { get; set; } = "LECT-2026";
        public static string AdminCode { get; set; } = "ADMIN-2026";

        public static string[] SecurityQuestions { get; private set; } = new string[]
        {
            "What is the name of your first pet?",
            "In which city were you born?",
            "What is the name of your favorite teacher?",
            "What was the name of your first school?"
        };

        private List<User> _users;

        public UserManager()
        {
            _users = new List<User>();
        }

        public UserManager(List<User> users)
        {
            _users = users;
        }

        public User Login(string username, string password)
        {
            foreach (User user in _users)
            {
                if (string.Equals(user.Username, username, StringComparison.OrdinalIgnoreCase)
                    && user.CheckPassword(password))
                {
                    return user;
                }
            }

            throw new WrongPasswordException(T("Wrong username or password!"));
        }

        public bool UsernameExists(string username)
        {
            return _users.Any(user => string.Equals(user.Username, username, StringComparison.OrdinalIgnoreCase));
        }

        public static bool IsValidUsername(string username)
        {
            if (username.Length < 3 || username.Length > 20)
            {
                return false;
            }

            return username.All(symbol => char.IsLetterOrDigit(symbol) || symbol == '.' || symbol == '_');
        }

        public User? FindByUsername(string username)
        {
            return _users.FirstOrDefault(user => string.Equals(user.Username, username, StringComparison.OrdinalIgnoreCase));
        }

        public int GetNextId()
        {
            if (_users.Count == 0)
            {
                return 1;
            }

            return _users.Max(user => user.Id) + 1;
        }

        public void CheckCode(UserRole role, string code)
        {
            string expected = role == UserRole.Admin ? AdminCode : LecturerCode;

            if (code.Trim() != expected)
                throw new InvalidCodeException(T("Wrong registration code!"));
        }

        public static void SetCodes(string lecturerCode, string adminCode)
        {
            lecturerCode = lecturerCode.Trim();
            adminCode = adminCode.Trim();

            foreach (string code in new string[] { lecturerCode, adminCode })
            {
                if (code.Length < 6 || code.Contains('|') || code.Contains(' '))
                    throw new ValidationException(T("A code needs at least 6 characters, without spaces or '|'!"));
            }

            if (lecturerCode == adminCode)
                throw new ValidationException(T("The two codes must be different!"));

            LecturerCode = lecturerCode;
            AdminCode = adminCode;
        }

        public Lecturer AddLecturer(string name, string username, string password, string subject, string question, string answer)
        {
            CheckNewUsername(username);

            Lecturer lecturer = new Lecturer(GetNextId(), name, username, subject);
            lecturer.SetPassword(password);

            if (question != string.Empty)
            {
                lecturer.SetSecurity(question, answer);
            }

            _users.Add(lecturer);

            return lecturer;
        }

        public Admin AddAdmin(string name, string username, string password, string question, string answer)
        {
            CheckNewUsername(username);

            Admin admin = new Admin(GetNextId(), name, username);
            admin.SetPassword(password);

            if (question != string.Empty)
            {
                admin.SetSecurity(question, answer);
            }

            _users.Add(admin);

            return admin;
        }

        private void CheckNewUsername(string username)
        {
            if (!IsValidUsername(username))
                throw new ValidationException(T("Username must have 3-20 characters: letters, digits, '.' or '_'!"));

            if (UsernameExists(username))
                throw new DuplicateUsernameException(T("Username {0} is already taken!", username));
        }

        public void ChangePassword(User user, string oldPassword, string newPassword)
        {
            if (!user.CheckPassword(oldPassword))
                throw new WrongPasswordException(T("The current password is wrong!"));

            user.SetPassword(newPassword);
        }

        public string ResetPassword(string username)
        {
            User? user = FindByUsername(username);

            if (user == null)
                throw new NotFoundException(T("User {0} not found!", username));

            string temporary = PasswordHelper.GenerateTemporary();
            user.SetPassword(temporary);

            return temporary;
        }

        public List<User> GetAllUsers()
        {
            return _users
                .OrderBy(user => user.Role)
                .ThenBy(user => user.Id)
                .ToList();
        }

        public List<Lecturer> GetAllLecturers()
        {
            return _users
                .OfType<Lecturer>()
                .ToList();
        }

        public Lecturer DeleteLecturer(string username)
        {
            foreach (Lecturer lecturer in GetAllLecturers())
            {
                if (string.Equals(lecturer.Username, username, StringComparison.OrdinalIgnoreCase))
                {
                    _users.Remove(lecturer);
                    return lecturer;
                }
            }

            throw new NotFoundException(T("Lecturer {0} not found!", username));
        }
    }
}
