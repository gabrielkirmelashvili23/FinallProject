using FinallProj.Exceptions;
using FinallProj.Helpers;
using FinallProj.Managers;
using FinallProj.Models;
using FinallProj.Models.Enums;
using Spectre.Console;
using static FinallProj.Helpers.Translator;

namespace FinallProj.Menus
{
    internal static class NewsMenu
    {
        public static void Show(User user)
        {
            List<MenuItem> items = new List<MenuItem>
            {
                new MenuItem("Read news", () => Read(user))
            };

            if (user.Role != UserRole.Student)
            {
                items.Add(new MenuItem("Publish news", () => Publish(user)));
                items.Add(new MenuItem("Edit news", () => Edit(user)));
                items.Add(new MenuItem("Delete news", () => Delete(user)));
            }

            Ui.RunMenu("University news", items);
        }

        private static string RoleText(UserRole role)
        {
            return role == UserRole.Admin ? T("Admin") : role == UserRole.Lecturer ? T("Lecturer") : T("Student");
        }

        private static string Label(NewsItem item)
        {
            return $"{Format.Date(item.Date)}  [{T(item.Category)}]  {item.Title}";
        }

        private static void Read(User user)
        {
            List<NewsItem> news = App.NewsManager.GetAll();

            if (news.Count == 0)
            {
                Ui.ShowInfo(T("No news yet."));
                return;
            }

            bool back = false;

            while (!back)
            {
                List<string> labels = news.Select(Label).ToList();
                labels.Add("Back");

                int choice = Ui.ChooseIndex("Choose a news item to read", labels, false, false, false);

                if (choice == news.Count)
                {
                    back = true;
                    continue;
                }

                ShowItem(news[choice]);
            }
        }

        private static void ShowItem(NewsItem item)
        {
            string text = $"{item.Text}\n\n{T("Category")}: {T(item.Category)}\n{T("Published")}: {Format.Date(item.Date)}\n{T("Author")}: {item.AuthorName} ({RoleText(item.AuthorRole)})";

            Panel panel = new Panel(Markup.Escape(text)).Header($"[{Theme.Title}]{Markup.Escape(item.Title)}[/]").Border(BoxBorder.Rounded).Padding(2, 1);
            Ui.Write(panel);
        }

        private static string ReadCategory()
        {
            int choice = Ui.ChooseIndex("Choose the category", NewsManager.Categories.ToList());

            return NewsManager.Categories[choice];
        }

        private static void Publish(User user)
        {
            string title = Ui.ReadText("Enter the title of the news:", 3);
            string text = Ui.ReadText("Enter the text of the news:", 10);
            string category = ReadCategory();

            NewsItem item = App.NewsManager.Add(user, title, text, category);
            App.SaveAll();
            App.Log($"news published: #{item.Id} by {user.Username}");

            Ui.ShowSuccess(T("News published!"));
        }

        private static NewsItem Choose(User user)
        {
            List<NewsItem> own = App.NewsManager.GetAll().Where(item => App.NewsManager.CanChange(user, item)).ToList();

            int index = Pickers.PickIndex("Choose a news item", own, Label);

            return own[index];
        }

        private static void Edit(User user)
        {
            NewsItem item = Choose(user);

            string title = Ui.ReadText("Enter the new title:", 3);
            string text = Ui.ReadText("Enter the new text:", 10);
            string category = ReadCategory();

            App.NewsManager.Edit(user, item.Id, title, text, category);
            App.SaveAll();
            App.Log($"news edited: #{item.Id} by {user.Username}");

            Ui.ShowSuccess(T("News updated!"));
        }

        private static void Delete(User user)
        {
            NewsItem item = Choose(user);

            if (!Ui.Confirm("Really delete this news?"))
            {
                Ui.ShowWarning(T("Deletion cancelled."));
                return;
            }

            App.NewsManager.Delete(user, item.Id);
            App.SaveAll();
            App.Log($"news deleted: #{item.Id} by {user.Username}");

            Ui.ShowSuccess(T("News deleted!"));
        }
    }
}
