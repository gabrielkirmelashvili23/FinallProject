using FinallProj.Exceptions;
using FinallProj.Models;
using Spectre.Console;
using Spectre.Console.Rendering;
using static FinallProj.Helpers.Translator;

namespace FinallProj.Helpers
{
    internal static class Ui
    {
        private const string Indent = "   ";

        public static void Line(string markup)
        {
            AnsiConsole.MarkupLine(Indent + markup);
        }

        public static void Write(IRenderable item)
        {
            AnsiConsole.Write(new Padder(item, new Padding(3, 0, 0, 0)));
        }

        public static void Title(string text)
        {
            AnsiConsole.Write(new Rule($"[bold {Theme.Title}]{Markup.Escape(Translator.Language == "ka" ? text : text.ToUpperInvariant())}[/]").RuleStyle(Theme.Muted));
            AnsiConsole.WriteLine();
            AnsiConsole.WriteLine();
        }

        public static void EndTask()
        {
            AnsiConsole.WriteLine();
            AnsiConsole.Write(new Rule().RuleStyle(Theme.Muted));
            AnsiConsole.WriteLine();
            AnsiConsole.WriteLine();
        }

        public static void ShowSuccess(string message)
        {
            AnsiConsole.WriteLine();
            Line($"[{Theme.Success}]{Markup.Escape(message)}[/]");
        }

        public static void ShowError(string message)
        {
            if (message.Length == 0)
            {
                ShowWarning(T("Cancelled."));
                return;
            }

            AnsiConsole.WriteLine();
            Line($"[{Theme.Error}]{Markup.Escape(T("Error:"))}[/] {Markup.Escape(message)}");
        }

        public static void ShowWarning(string message)
        {
            AnsiConsole.WriteLine();
            Line($"[{Theme.Warn}]{Markup.Escape(message)}[/]");
        }

        public static void ShowInfo(string message)
        {
            Line($"[{Theme.Muted}]{Markup.Escape(message)}[/]");
        }

        public static void ShowReport(string report)
        {
            Panel panel = new Panel(Markup.Escape(report)).Header($"[{Theme.Title}]{Markup.Escape(T("report card"))}[/]").Border(BoxBorder.Rounded);
            Write(panel);
        }

        public static int ChooseIndex(string title, List<string> labels, bool asTitle = false, bool icons = false, bool rawMarkup = false, bool allowBack = true)
        {
            bool addBack = allowBack && !labels.Any(label => label == "Back" || label.StartsWith("Back to"));
            int total = addBack ? labels.Count + 1 : labels.Count;
            List<int> indexes = new List<int>();

            for (int i = 0; i < total; i++)
            {
                indexes.Add(i);
            }

            if (asTitle)
            {
                Title(T(title));
            }
            else
            {
                AnsiConsole.WriteLine();
                Line($"[{Theme.Accent}]{Markup.Escape(T(title))}[/]");
                AnsiConsole.WriteLine();
            }

            int choice = AnsiConsole.Prompt(
                new SelectionPrompt<int>()
                    .PageSize(14)
                    .HighlightStyle(new Style(Theme.TitleColor))
                    .UseConverter(index => index == labels.Count ? FormatChoice("Back", icons, false) : FormatChoice(labels[index], icons, rawMarkup))
                    .AddChoices(indexes));

            AnsiConsole.WriteLine();

            if (addBack && choice == labels.Count)
            {
                throw new BackException();
            }

            return choice;
        }

        private static string FormatChoice(string label, bool icons, bool rawMarkup)
        {
            if (rawMarkup)
            {
                return "  " + label;
            }

            string text = Markup.Escape(T(label));

            if (!icons || !Icons.Enabled)
            {
                return " " + text;
            }

            return $" [{Theme.Accent}]{Markup.Escape(Icons.For(label))}[/]  {text}";
        }

        public static void RunMenu(string title, List<MenuItem> items, string exitText = "Back")
        {
            bool back = false;

            do
            {
                List<string> labels = items.Select(item => item.Title).ToList();
                labels.Add(exitText);

                int choice = ChooseIndex(title, labels, true, true, false, false);

                if (choice == items.Count)
                {
                    back = true;
                }
                else
                {
                    try
                    {
                        items[choice].Run();
                    }
                    catch (AppException ex)
                    {
                        ShowError(ex.Message);
                    }
                    catch (Exception ex)
                    {
                        ShowError(T("Unexpected error: {0}", ex.Message));
                    }
                    finally
                    {
                        EndTask();
                    }
                }
            } while (!back);
        }

        private static string PromptLabel(string message, string color)
        {
            Line($"[{color}]{Markup.Escape(T(message))}[/]  [{Theme.Muted}]{Markup.Escape(T("(type back to cancel)"))}[/]");
            AnsiConsole.WriteLine();

            return $"[{Theme.Muted}]>[/]";
        }

        private static bool IsBack(string text)
        {
            string value = text.Trim().ToLowerInvariant();

            return value == "back" || value == "უკან" || value == "назад";
        }

        private static string ReadRaw(string message, Func<string, bool> isValid, string error, string? defaultValue = null, bool allowEmpty = false)
        {
            TextPrompt<string> prompt = new TextPrompt<string>(PromptLabel(message, Theme.Accent))
                .ValidationErrorMessage(ErrorText(error))
                .Validate(input => IsBack(input) || isValid(input));

            if (defaultValue != null)
            {
                prompt.DefaultValue(defaultValue);
            }

            if (allowEmpty)
            {
                prompt.AllowEmpty();
            }

            string text = AnsiConsole.Prompt(prompt).Trim();
            AnsiConsole.WriteLine();

            if (IsBack(text))
            {
                throw new BackException();
            }

            return text;
        }

        private static string ErrorText(string text)
        {
            return $"[{Theme.Error}]{Markup.Escape(text)}[/]";
        }

        public static string ReadLine(string message)
        {
            return ReadRaw(message, input => true, string.Empty);
        }

        public static int ReadPositiveNumber(string message)
        {
            string text = ReadRaw(message, input => int.TryParse(input, out int value) && value > 0, T("Invalid input, enter a positive number."));

            return int.Parse(text);
        }

        public static int ReadNumberInRange(string message, int min, int max)
        {
            string text = ReadRaw(message, input => int.TryParse(input, out int value) && value >= min && value <= max, T("The number must be between {0} and {1}.", min, max));

            return int.Parse(text);
        }

        public static decimal ReadMoney(string message)
        {
            string text = ReadRaw(message, input => decimal.TryParse(input, out decimal value) && value > 0, T("Invalid input, enter a positive number."));

            return decimal.Parse(text);
        }

        public static string ReadText(string message, int minLength)
        {
            return ReadRaw(message, input => input.Trim().Length >= minLength && !input.Contains("|"), T("The text must have at least {0} characters and no '|' symbol.", minLength));
        }

        public static string ReadSecret(string message)
        {
            TextPrompt<string> prompt = new TextPrompt<string>(PromptLabel(message, Theme.Success)).Secret().AllowEmpty();
            string secret = AnsiConsole.Prompt(prompt);
            AnsiConsole.WriteLine();

            if (IsBack(secret))
            {
                throw new BackException();
            }

            return secret;
        }

        public static DateTime ReadDate(string message, DateTime defaultDate)
        {
            string text = ReadRaw(message, input => Format.TryParseDate(input, out DateTime _), T("Use the format yyyy-MM-dd."), Format.Date(defaultDate));

            return Format.ParseDate(text);
        }

        public static string ReadTime(string message, string defaultTime)
        {
            string text = ReadRaw(message, input => TimeText.TryParse(input, out TimeSpan _), T("Use the time format HH:mm, for example 09:30."), defaultTime);

            return TimeText.Normalize(text);
        }

        public static string ReadOptional(string message)
        {
            return ReadRaw(message, input => true, string.Empty, null, true);
        }

        public static int ReadSignedNumber(string message, int min, int max)
        {
            string text = ReadRaw(message, input => int.TryParse(input, out int value) && value >= min && value <= max && value != 0, T("The number must be between {0} and {1}.", min, max));

            return int.Parse(text);
        }

        public static bool Ask(string message, bool defaultValue)
        {
            bool answer = AnsiConsole.Confirm(PromptLabel(message, Theme.Accent), defaultValue);
            AnsiConsole.WriteLine();

            return answer;
        }

        public static bool Confirm(string message)
        {
            bool answer = AnsiConsole.Confirm(PromptLabel(message, Theme.Error), false);
            AnsiConsole.WriteLine();

            return answer;
        }

        public static char ReadGrade(string title)
        {
            List<string> labels = new List<string> { "A", "B", "C", "D", "E", "F" };
            int choice = ChooseIndex(title, labels);

            return labels[choice][0];
        }

        public static string StrengthBar(string password)
        {
            int score = PasswordHelper.GetStrength(password);

            string color = Theme.Success;
            string label = T("strong");

            if (score <= 1)
            {
                color = Theme.Error;
                label = T("weak");
            }
            else if (score <= 3)
            {
                color = Theme.Warn;
                label = T("medium");
            }

            return $"[{color}]{new string('█', score)}[/][{Theme.Muted}]{new string('░', 4 - score)}[/] {Markup.Escape(T("Password strength"))}: {Markup.Escape(label)}";
        }
    }
}
