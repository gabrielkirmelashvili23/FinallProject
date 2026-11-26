namespace FinallProj.Helpers
{
    internal static class DataStore
    {
        public static string Clean(string text)
        {
            return text.Replace("|", "/").Replace("\r", " ").Replace("\n", " ").Trim();
        }

        public static List<T> Load<T>(string path, Func<string, T> parse, string label)
        {
            List<T> items = new List<T>();

            if (!File.Exists(path))
            {
                return items;
            }

            string[] lines = File.ReadAllLines(path);

            for (int i = 0; i < lines.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lines[i]))
                {
                    continue;
                }

                try
                {
                    items.Add(parse(lines[i]));
                }
                catch (Exception ex)
                {
                    Console.WriteLine(Translator.T("{0} file: line {1} is damaged and skipped ({2})", label, i + 1, ex.Message));
                }
            }

            return items;
        }

        public static void Save<T>(string path, List<T> items, Func<T, string> toLine)
        {
            string? folder = Path.GetDirectoryName(path);

            if (!string.IsNullOrEmpty(folder) && !Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }

            File.WriteAllLines(path, items.Select(toLine).ToList());
        }
    }
}
