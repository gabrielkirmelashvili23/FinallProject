using FinallProj.Helpers;
using FinallProj.Models;
using Spectre.Console;
using static FinallProj.Helpers.Translator;

namespace FinallProj.Menus
{
    internal static class AccountMenu
    {
        public static void Show(User user)
        {
            List<MenuItem> items = new List<MenuItem>
            {
                new MenuItem("Change password", () => ChangePassword(user)),
                new MenuItem("Set security question", () => SetSecurityQuestion(user)),
                new MenuItem("Language", ChooseLanguage),
                new MenuItem("Background", ChooseBackground),
                new MenuItem("Fonts", FontMenu.Show)
            };

            Ui.RunMenu("My account", items);
        }

        public static void SettingsMenu()
        {
            List<MenuItem> items = new List<MenuItem>
            {
                new MenuItem("Language", ChooseLanguage),
                new MenuItem("Background", ChooseBackground),
                new MenuItem("Icons", ToggleIcons),
                new MenuItem("Fonts", FontMenu.Show)
            };

            Ui.RunMenu("Settings", items);
        }

        public static void ChangePassword(User user)
        {
            string oldPassword = Ui.ReadSecret("Enter your current password:");

            if (!user.CheckPassword(oldPassword))
            {
                App.Log($"failed password change: {user.Username}");
                Ui.ShowError(T("The current password is wrong!"));
                return;
            }

            string newPassword = AuthMenu.ReadNewPassword();
            App.UserManager.ChangePassword(user, oldPassword, newPassword);

            App.SaveAll();
            App.Log($"password changed: {user.Username}");

            Ui.ShowSuccess(T("Password changed successfully!"));
        }

        public static void SetSecurityQuestion(User user)
        {
            string password = Ui.ReadSecret("Enter your current password:");

            if (!user.CheckPassword(password))
            {
                Ui.ShowError(T("The current password is wrong!"));
                return;
            }

            AuthMenu.ReadSecurityQuestion(out string question, out string answer);
            user.SetSecurity(question, answer);

            App.SaveAll();
            App.Log($"security question set: {user.Username}");

            Ui.ShowSuccess(T("Security question saved!"));
        }

        public static void ToggleIcons()
        {
            Icons.Enabled = !Icons.Enabled;
            App.SaveSettings();

            Ui.ShowSuccess(T(Icons.Enabled ? "Icons are on." : "Icons are off."));
        }

        public static void ChooseBackground()
        {
            int choice = Ui.ChooseIndex("Choose a background", Background.Swatches(), false, false, true);

            Background.Set(Background.Keys[choice]);
            App.SaveSettings();

            Ui.ShowSuccess(T("Background changed!"));
        }

        public static void ChooseLanguage()
        {
            int choice = Ui.ChooseIndex("Choose a language", Translator.LanguageNames.ToList());

            Translator.SetLanguage(Translator.LanguageCodes[choice]);
            App.SaveSettings();

            Ui.ShowSuccess(T("Language changed!"));
        }
    }
}
