using FinallProj.Helpers;
using FinallProj.Models;
using static FinallProj.Helpers.Translator;

namespace FinallProj.Menus
{
    internal static class AcademicMenu
    {
        public static void Show(User user)
        {
            List<MenuItem> items = new List<MenuItem>
            {
                new MenuItem("Lectures", () => ScheduleMenu.ShowStaff(user)),
                new MenuItem("Exams", () => ExamMenu.ShowStaff(user)),
                new MenuItem("Assignments", () => AssignmentMenu.ShowStaff(user)),
                new MenuItem("Points and rating", () => ScoreMenu.ShowStaff(user)),
                new MenuItem("Semesters and GPA", () => GpaMenu.ShowStaff(user))
            };

            Ui.RunMenu("Academic", items);
        }
    }
}
