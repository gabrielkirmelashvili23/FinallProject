using FinallProj.Exceptions;
using FinallProj.Helpers;
using FinallProj.Models;
using FinallProj.Models.Enums;
using static FinallProj.Helpers.Translator;

namespace FinallProj.Managers
{
    internal class NewsManager
    {
        public static string[] Categories { get; private set; } = { "General", "Events", "Exams", "Schedule", "Scholarships" };

        private List<NewsItem> _news;

        public NewsManager(List<NewsItem> news)
        {
            _news = news;
        }

        public List<NewsItem> GetAll()
        {
            return _news.OrderByDescending(item => item.Date).ThenByDescending(item => item.Id).ToList();
        }

        public NewsItem Find(int id)
        {
            NewsItem? item = _news.FirstOrDefault(news => news.Id == id);

            if (item == null)
                throw new NotFoundException(T("News not found!"));

            return item;
        }

        public NewsItem Add(User author, string title, string text, string category)
        {
            if (author.Role == UserRole.Student)
                throw new ValidationException(T("Only the admin and lecturers can publish news!"));

            Check(title, text, category);

            NewsItem item = new NewsItem
            {
                Id = _news.Count == 0 ? 1 : _news.Max(news => news.Id) + 1,
                Title = DataStore.Clean(title),
                Text = DataStore.Clean(text),
                Category = category,
                Date = Clock.Today,
                AuthorUsername = author.Username,
                AuthorName = author.Name,
                AuthorRole = author.Role
            };

            _news.Add(item);

            return item;
        }

        public NewsItem Edit(User user, int id, string title, string text, string category)
        {
            NewsItem item = Find(id);
            CheckRights(user, item);
            Check(title, text, category);

            item.Title = DataStore.Clean(title);
            item.Text = DataStore.Clean(text);
            item.Category = category;

            return item;
        }

        public NewsItem Delete(User user, int id)
        {
            NewsItem item = Find(id);
            CheckRights(user, item);

            _news.Remove(item);

            return item;
        }

        public bool CanChange(User user, NewsItem item)
        {
            return user.Role == UserRole.Admin || (user.Role == UserRole.Lecturer && item.AuthorUsername == user.Username);
        }

        private void CheckRights(User user, NewsItem item)
        {
            if (!CanChange(user, item))
                throw new ValidationException(T("Only the admin or the author can change this news!"));
        }

        private void Check(string title, string text, string category)
        {
            if (title.Trim().Length < 3)
                throw new ValidationException(T("The title must have at least 3 characters."));

            if (text.Trim().Length < 10)
                throw new ValidationException(T("The news text must have at least 10 characters."));

            if (Array.IndexOf(Categories, category) < 0)
                throw new ValidationException(T("Unknown category!"));
        }
    }
}
