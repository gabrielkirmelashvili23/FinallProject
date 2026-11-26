namespace FinallProj.Models
{
    internal class MenuItem
    {
        public string Title { get; set; } = string.Empty;
        public Action Run { get; set; }

        public MenuItem(string title, Action run)
        {
            Title = title;
            Run = run;
        }
    }
}
