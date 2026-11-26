using FinallProj.Helpers;
using FinallProj.Models;
using Spectre.Console;
using static FinallProj.Helpers.Translator;

namespace FinallProj.Menus
{
    internal static class FontMenu
    {
        public static void Show()
        {
            List<MenuItem> items = new List<MenuItem>
            {
                new MenuItem("Choose a font", () => Gallery(FontCatalog.Main, false)),
                new MenuItem("Choose the Georgian letters font", () => Gallery(FontCatalog.Georgian, true)),
                new MenuItem("Bold text", ChooseBold),
                new MenuItem("Font sample and status", ShowSample)
            };

            Ui.RunMenu("Fonts", items);
        }

        private static void Gallery(List<FontEntry> catalog, bool georgian)
        {
            if (FontSetup.SettingsPath().Length == 0)
            {
                ShowHowTo();
                return;
            }

            while (true)
            {
                string current = georgian ? FontSetup.CurrentGeorgian() : FontSetup.CurrentMain();
                List<FontEntry> offered = catalog.Where(FontSetup.CanOffer).ToList();
                List<string> labels = new List<string>();

                foreach (FontEntry font in offered)
                {
                    string mark = font.Face == current ? $"[{Theme.Success}]✓[/]" : " ";
                    string note = font.Monospaced ? string.Empty : $" [{Theme.Warn}]{Markup.Escape(T("(not monospaced)"))}[/]";
                    labels.Add($"{mark} {Markup.Escape(font.Face)}  [dim]{Markup.Escape(T(font.Style))}[/]{note}");
                }

                labels.Add("   " + Markup.Escape(T("Back")));

                int choice = Ui.ChooseIndex(georgian ? "Choose the Georgian letters font" : "Choose a font", labels, true, false, true, false);

                if (choice == offered.Count)
                {
                    return;
                }

                FontEntry picked = offered[choice];
                string main = georgian ? FontSetup.CurrentMain() : picked.Face;
                string geo = georgian ? picked.Face : FontSetup.CurrentGeorgian();

                try
                {
                    FontSetup.Install(picked);
                    FontSetup.ApplyToTerminal(main, geo);
                    App.Log("terminal font changed");
                }
                catch (Exceptions.AppException ex)
                {
                    Ui.ShowError(ex.Message);
                    Ui.EndTask();
                }
            }
        }

        private static void ChooseBold()
        {
            if (FontSetup.SettingsPath().Length == 0)
            {
                ShowHowTo();
                return;
            }

            bool bold = FontSetup.IsBold();
            string on = (bold ? $"[{Theme.Success}]✓[/] " : "  ") + Markup.Escape(T("Bold"));
            string off = (!bold ? $"[{Theme.Success}]✓[/] " : "  ") + Markup.Escape(T("Normal"));

            int choice = Ui.ChooseIndex("Bold text", new List<string> { on, off }, false, false, true);

            try
            {
                FontSetup.ApplyBold(choice == 0);
                App.Log("terminal bold changed");
            }
            catch (Exceptions.AppException ex)
            {
                Ui.ShowError(ex.Message);
            }
        }

        private static void ShowSample()
        {
            string status;

            if (!FontSetup.IsSupportedSystem())
            {
                status = T("Automatic check works only on Windows.");
            }
            else
            {
                status = FontSetup.IsInstalled() ? T("The font {0} is installed.", FontSetup.FontName) : T("The font {0} is NOT installed yet.", FontSetup.FontName);
            }

            string sample =
                "English: The quick brown fox jumps over the lazy dog 0123456789\n" +
                "ქართული: ღვინო, მზე და სიყვარული - გამარჯობა, სტუდენტო!\n" +
                "Русский: Съешь ещё этих мягких французских булок\n" +
                "Symbols: ➜ ✚ ⚙ ★ ▤ ¤ ☻ ✖ ↩";

            Ui.Write(new Panel(Markup.Escape(sample)).Header($"[{Theme.Title}]{Markup.Escape(T("Font sample"))}[/]").Border(BoxBorder.Rounded).Padding(2, 1));
            Ui.Line(Markup.Escape(status));

            if (FontSetup.SettingsPath().Length > 0)
            {
                Ui.Line(Markup.Escape(T("Current font: {0}", FontSetup.CurrentFace())));
            }

            Ui.ShowInfo(T("Recommended font for all languages: {0}", FontSetup.FontName));
        }

        private static void ShowHowTo()
        {
            string text =
                T("Windows Terminal: open Settings, then Profiles, then Defaults, then Appearance, set Font face to {0}, press Save.", FontSetup.FontName) + "\n\n" +
                T("Visual Studio Code terminal: Settings, search terminal.integrated.fontFamily, enter {0}.", FontSetup.FontName) + "\n\n" +
                T("Classic Windows console: right-click the title bar, Properties, Font tab, choose a font that supports Georgian.") + "\n\n" +
                T("Why: the program writes Unicode (UTF-8), but the terminal draws the letters, so one font with Latin, Georgian and Cyrillic letters keeps all languages readable.");

            Ui.Write(new Panel(Markup.Escape(text)).Header($"[{Theme.Title}]{Markup.Escape(T("Select the font in the terminal"))}[/]").Border(BoxBorder.Rounded).Padding(2, 1));
        }
    }
}
