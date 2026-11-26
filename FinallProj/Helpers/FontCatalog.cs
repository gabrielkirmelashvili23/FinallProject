namespace FinallProj.Helpers
{
    internal class FontEntry
    {
        public string Face { get; }
        public string Style { get; }
        public string[] Files { get; }
        public bool Bundled { get; }
        public bool Monospaced { get; }

        public FontEntry(string face, string style, bool bundled, bool monospaced, params string[] files)
        {
            Face = face;
            Style = style;
            Bundled = bundled;
            Monospaced = monospaced;
            Files = files;
        }
    }

    internal static class FontCatalog
    {
        public const string Fallback = "DejaVu Sans Mono";

        public static readonly List<FontEntry> Main = new List<FontEntry>
        {
            new FontEntry("DejaVu Sans Mono", "classic", true, true, "DejaVuSansMono.ttf", "DejaVuSansMono-Bold.ttf"),
            new FontEntry("Comic Mono", "funny", true, true, "ComicMono.ttf", "ComicMono-Bold.ttf"),
            new FontEntry("VT323", "retro pixel", true, true, "VT323-Regular.ttf"),
            new FontEntry("Space Mono", "quirky retro", true, true, "SpaceMono-Regular.ttf"),
            new FontEntry("Major Mono Display", "quirky display", true, true, "MajorMonoDisplay-Regular.ttf"),
            new FontEntry("Syne Mono", "artistic", true, true, "SyneMono-Regular.ttf"),
            new FontEntry("Cutive Mono", "elegant typewriter", true, true, "CutiveMono-Regular.ttf"),
            new FontEntry("Courier Prime", "typewriter", true, true, "CourierPrime-Regular.ttf"),
            new FontEntry("Xanh Mono", "elegant serif", true, true, "XanhMono-Regular.ttf"),
            new FontEntry("Ubuntu Mono", "rounded and friendly", true, true, "UbuntuMono-Regular.ttf"),
            new FontEntry("DM Mono", "soft and rounded", true, true, "DMMono-Regular.ttf"),
            new FontEntry("Roboto Mono", "rounded modern", true, true, "RobotoMono-Regular.ttf"),
            new FontEntry("Share Tech Mono", "techno", true, true, "ShareTechMono-Regular.ttf"),
            new FontEntry("NovaMono", "techno", true, true, "NovaMono.ttf"),
            new FontEntry("Martian Mono", "wide", true, true, "MartianMono-Regular.ttf"),
            new FontEntry("JetBrains Mono", "modern", true, true, "JetBrainsMono-Regular.ttf"),
            new FontEntry("Fira Code", "modern", true, true, "FiraCode-Regular.ttf"),
            new FontEntry("Red Hat Mono", "modern", true, true, "RedHatMono-Regular.ttf"),
            new FontEntry("IBM Plex Mono", "classic", true, true, "IBMPlexMono-Regular.ttf"),
            new FontEntry("Source Code Pro", "classic", true, true, "SourceCodePro-Regular.ttf"),
            new FontEntry("Inconsolata", "classic", true, true, "Inconsolata-Regular.ttf"),
            new FontEntry("Anonymous Pro", "classic", true, true, "AnonymousPro-Regular.ttf"),
            new FontEntry("Cascadia Mono", "modern", false, true, "CascadiaMono.ttf"),
            new FontEntry("Consolas", "classic", false, true, "consola.ttf"),
            new FontEntry("Courier New", "typewriter", false, true, "cour.ttf"),
            new FontEntry("Lucida Console", "classic", false, true, "lucon.ttf"),
            new FontEntry("Segoe Script", "cursive italic", false, false, "segoesc.ttf"),
            new FontEntry("Segoe Print", "handwriting", false, false, "segoepr.ttf"),
            new FontEntry("Ink Free", "handwriting", false, false, "Inkfree.ttf"),
            new FontEntry("Comic Sans MS", "funny", false, false, "comic.ttf"),
            new FontEntry("Gabriola", "elegant script", false, false, "Gabriola.ttf")
        };

        public static readonly List<FontEntry> Georgian = new List<FontEntry>
        {
            new FontEntry("DejaVu Sans Mono", "monospaced, keeps tables aligned", true, true, "DejaVuSansMono.ttf", "DejaVuSansMono-Bold.ttf"),
            new FontEntry("Noto Sans Georgian", "clean sans", true, false, "NotoSansGeorgian-Regular.ttf"),
            new FontEntry("Noto Serif Georgian", "elegant serif", true, false, "NotoSerifGeorgian-Regular.ttf"),
            new FontEntry("Sylfaen", "classic Windows serif", false, false, "sylfaen.ttf"),
            new FontEntry("Segoe UI", "Windows interface font", false, false, "segoeui.ttf"),
            new FontEntry("Calibri", "Windows office font", false, false, "calibri.ttf")
        };
    }
}
