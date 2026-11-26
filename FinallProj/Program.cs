using FinallProj;
using FinallProj.Helpers;
using FinallProj.Menus;
using FinallProj.Models;
using Spectre.Console;
using static FinallProj.Helpers.Translator;

App.Start();

AnsiConsole.WriteLine();
AnsiConsole.Write(new FigletText("Student System").Centered().Color(Theme.AccentColor));
AnsiConsole.WriteLine();
Ui.Title(T("STUDENT MANAGEMENT SYSTEM"));
Ui.ShowInfo(T("Loaded {0} users, {1} courses.", App.Users.Count, App.Courses.Count));

if (App.Users.Count == 0)
{
    Ui.ShowWarning(T("No users yet. Choose 'Register' to create the first account (see README for the staff codes)."));
}

AnsiConsole.WriteLine();
AnsiConsole.WriteLine();

List<MenuItem> mainMenu = new List<MenuItem>
{
    new MenuItem("Log in", AuthMenu.LoginMenu),
    new MenuItem("Register", AuthMenu.RegisterMenu),
    new MenuItem("Forgot password", AuthMenu.ForgotPasswordMenu),
    new MenuItem("Settings", AccountMenu.SettingsMenu)
};

Ui.RunMenu("Main menu", mainMenu, "Exit");

Background.Reset();
Ui.ShowWarning(T("Goodbye!"));
AnsiConsole.WriteLine();
