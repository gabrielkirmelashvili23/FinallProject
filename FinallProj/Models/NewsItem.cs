using FinallProj.Helpers;
using FinallProj.Models.Enums;

namespace FinallProj.Models
{
    internal class NewsItem
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public string Category { get; set; } = "General";
        public DateTime Date { get; set; }
        public string AuthorUsername { get; set; } = string.Empty;
        public string AuthorName { get; set; } = string.Empty;
        public UserRole AuthorRole { get; set; }

        public string ToFileLine()
        {
            return $"{Id}|{DataStore.Clean(Title)}|{DataStore.Clean(Text)}|{DataStore.Clean(Category)}|{Format.Date(Date)}|{AuthorUsername}|{DataStore.Clean(AuthorName)}|{(int)AuthorRole}";
        }

        public static NewsItem FromFileLine(string line)
        {
            string[] p = line.Split('|');

            return new NewsItem
            {
                Id = int.Parse(p[0]),
                Title = p[1],
                Text = p[2],
                Category = p[3],
                Date = Format.ParseDate(p[4]),
                AuthorUsername = p[5],
                AuthorName = p[6],
                AuthorRole = (UserRole)int.Parse(p[7])
            };
        }
    }
}
