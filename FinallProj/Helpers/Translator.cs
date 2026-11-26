namespace FinallProj.Helpers
{
    internal static class Translator
    {
        static Translator()
        {
            foreach (KeyValuePair<string, string> pair in TranslationsAcademic.Georgian)
            {
                Translations.Georgian.TryAdd(pair.Key, pair.Value);
            }

            foreach (KeyValuePair<string, string> pair in TranslationsAcademic.Russian)
            {
                Translations.Russian.TryAdd(pair.Key, pair.Value);
            }
        }

        public static string Language { get; private set; } = "en";

        public static string[] LanguageCodes { get; private set; } = { "en", "ka", "ru" };

        public static string[] LanguageNames { get; private set; } = { "English", "ქართული", "Русский" };

        public static void SetLanguage(string code)
        {
            if (Array.IndexOf(LanguageCodes, code) >= 0)
            {
                Language = code;
            }
        }

        public static string T(string text, params object[] args)
        {
            string result = text;
            Dictionary<string, string>? dictionary = null;

            if (Language == "ka")
            {
                dictionary = Translations.Georgian;
            }
            else if (Language == "ru")
            {
                dictionary = Translations.Russian;
            }

            if (dictionary != null && dictionary.TryGetValue(text, out string? translated))
            {
                result = translated;
            }

            if (args.Length > 0)
            {
                result = string.Format(result, args);
            }

            return result;
        }
    }
}
