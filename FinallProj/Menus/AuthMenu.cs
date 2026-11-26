using FinallProj.Exceptions;
using FinallProj.Helpers;
using FinallProj.Managers;
using FinallProj.Models;
using FinallProj.Models.Enums;
using Spectre.Console;
using static FinallProj.Helpers.Translator;

namespace FinallProj.Menus
{
    internal static class AuthMenu
    {
        public static void LoginMenu()
        {
            int maxAttempts = 3;

            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                Ui.Title(T("Log in"));

                string username = Ui.ReadOptional("Enter username (leave empty to go back):");

                if (username.Length == 0)
                {
                    return;
                }

                string password = Ui.ReadSecret("Enter password:");

                try
                {
                    User user = App.UserManager.Login(username, password);

                    App.Log($"login: {user.Username} ({user.Role})");
                    Screens.ShowWelcome(user);

                    switch (user.Role)
                    {
                        case UserRole.Admin:
                        case UserRole.Lecturer:
                            StaffMenu.Show(user);
                            break;
                        case UserRole.Student:
                            StudentMenu.Show((Student)user);
                            break;
                    }

                    return;
                }
                catch (WrongPasswordException ex)
                {
                    App.Log($"failed login: {username}");
                    Ui.ShowError(T("{0} (attempts left: {1})", ex.Message, maxAttempts - attempt));

                    if (attempt < maxAttempts)
                    {
                        int next = Ui.ChooseIndex("What do you want to do?", new List<string> { "Try again", "Register", "Back to the main menu" });

                        if (next == 1)
                        {
                            RegisterMenu();
                            return;
                        }

                        if (next == 2)
                        {
                            return;
                        }
                    }
                    else
                    {
                        Ui.EndTask();
                    }
                }
            }

            Ui.ShowError(T("Attempt limit reached, returning to the main menu."));
        }

        public static void RegisterMenu()
        {
            Ui.Title(T("Register"));

            List<string> roles = new List<string> { "Student", "Lecturer", "Admin", "Back" };
            int roleChoice = Ui.ChooseIndex("Register as", roles);

            if (roleChoice == 3)
            {
                return;
            }

            UserRole role = UserRole.Student;

            if (roleChoice == 1)
            {
                role = UserRole.Lecturer;
            }
            else if (roleChoice == 2)
            {
                role = UserRole.Admin;
            }

            try
            {
                if (role != UserRole.Student)
                {
                    Ui.ShowInfo(T("Lecturer and Admin accounts need a registration code from the school administration. An Admin can see or change the codes in Users and security."));
                    string code = Ui.ReadSecret("Enter the registration code:");
                    App.UserManager.CheckCode(role, code);
                }

                string name = Ui.ReadText("Enter your full name:", 2);
                string username = ReadFreeUsername();
                string password = ReadNewPassword();

                ReadSecurityQuestion(out string question, out string answer);

                User user;

                if (role == UserRole.Student)
                {
                    user = App.StudentManager.AddStudent(name, username, password, question, answer);
                }
                else if (role == UserRole.Lecturer)
                {
                    string subject = Ui.ReadText("Enter the subject you teach:", 2);
                    user = App.UserManager.AddLecturer(name, username, password, subject, question, answer);
                }
                else
                {
                    user = App.UserManager.AddAdmin(name, username, password, question, answer);
                }

                App.SaveAll();
                App.Log($"registered: {user.Username} ({user.Role})");

                Ui.ShowSuccess(T("Registration completed! You can log in now."));

                if (user is Student student)
                {
                    Ui.Line(Markup.Escape(T("Your roll number: {0}", student.RollNumber)));
                }
            }
            catch (InvalidCodeException ex)
            {
                App.Log("failed registration code");
                Ui.ShowError(ex.Message);
            }
            catch (AppException ex)
            {
                Ui.ShowError(ex.Message);
            }
        }

        public static string ReadFreeUsername()
        {
            while (true)
            {
                string username = Ui.ReadText("Choose a username (3-20 characters, letters, digits, . or _):", 3);

                if (!UserManager.IsValidUsername(username))
                {
                    Ui.ShowError(T("Username must have 3-20 characters: letters, digits, '.' or '_'!"));
                }
                else if (App.UserManager.UsernameExists(username))
                {
                    Ui.ShowError(T("Username {0} is already taken!", username));
                }
                else
                {
                    return username;
                }
            }
        }

        public static string ReadNewPassword()
        {
            while (true)
            {
                string password = Ui.ReadSecret("Enter a new password (min 8 characters, a letter and a digit):");

                if (!PasswordHelper.IsValid(password))
                {
                    Ui.ShowError(T("Password must have at least 8 characters, a letter and a digit!"));
                    continue;
                }

                Ui.Line(Ui.StrengthBar(password));

                string repeated = Ui.ReadSecret("Repeat the password:");

                if (repeated != password)
                {
                    Ui.ShowError(T("Passwords do not match!"));
                    continue;
                }

                return password;
            }
        }

        public static void ReadSecurityQuestion(out string question, out string answer)
        {
            List<string> questions = UserManager.SecurityQuestions.ToList();
            int choice = Ui.ChooseIndex("Choose a security question (used to recover the password)", questions);

            question = questions[choice];
            answer = Ui.ReadText("Your answer:", 2);
        }

        public static void ForgotPasswordMenu()
        {
            string username = Ui.ReadText("Enter your username:", 3);
            User? user = App.UserManager.FindByUsername(username);

            if (user == null || !user.HasSecurityQuestion())
            {
                Ui.ShowWarning(T("Password recovery is not available for this account. Ask an administrator to reset the password."));
                return;
            }

            Ui.Line($"[{Theme.Accent}]{Markup.Escape(T(user.SecurityQuestion))}[/]");

            for (int attempt = 1; attempt <= 3; attempt++)
            {
                string answer = Ui.ReadSecret("Your answer:");

                if (user.CheckSecurityAnswer(answer))
                {
                    string newPassword = ReadNewPassword();
                    user.SetPassword(newPassword);

                    App.SaveAll();
                    App.Log($"password recovered: {user.Username}");

                    Ui.ShowSuccess(T("Password changed successfully!"));
                    return;
                }

                App.Log($"failed recovery answer: {user.Username}");
                Ui.ShowError(T("Wrong answer! (attempts left: {0})", 3 - attempt));
            }
        }
    }
}
