using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Win32;

namespace FinallProj.Helpers
{
    internal static class FontSetup
    {
        public const string FontName = "DejaVu Sans Mono";

        private static string[] _files = { "DejaVuSansMono.ttf", "DejaVuSansMono-Bold.ttf" };
        private static string[] _registryNames = { "DejaVu Sans Mono (TrueType)", "DejaVu Sans Mono Bold (TrueType)" };

        public static string SourceFolder
        {
            get
            {
                return Path.Combine(AppContext.BaseDirectory, "Fonts");
            }
        }

        public static bool IsSupportedSystem()
        {
            return OperatingSystem.IsWindows();
        }

        public static bool FilesAvailable()
        {
            return _files.All(file => File.Exists(Path.Combine(SourceFolder, file)));
        }

        public static string UserFontsFolder()
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Microsoft", "Windows", "Fonts");
        }

        private static string SystemFontsFolder()
        {
            return Environment.GetFolderPath(Environment.SpecialFolder.Fonts);
        }

        public static bool IsInstalled()
        {
            return IsAvailable(FontCatalog.Main[0]);
        }

        public static bool IsAvailable(FontEntry font)
        {
            if (!OperatingSystem.IsWindows())
            {
                return false;
            }

            string first = font.Files[0];

            return File.Exists(Path.Combine(UserFontsFolder(), first)) || File.Exists(Path.Combine(SystemFontsFolder(), first));
        }

        public static bool CanOffer(FontEntry font)
        {
            return IsAvailable(font) || (font.Bundled && font.Files.All(file => File.Exists(Path.Combine(SourceFolder, file))));
        }

        public static void InstallForCurrentUser()
        {
            Install(FontCatalog.Main[0]);
        }

        public static void Install(FontEntry font)
        {
            if (!OperatingSystem.IsWindows())
                throw new Exceptions.ValidationException(Translator.T("Automatic installation works only on Windows."));

            if (IsAvailable(font) || !font.Bundled)
            {
                return;
            }

            if (!font.Files.All(file => File.Exists(Path.Combine(SourceFolder, file))))
                throw new Exceptions.NotFoundException(Translator.T("The font files were not found next to the program (Fonts folder)!"));

            string target = UserFontsFolder();
            Directory.CreateDirectory(target);

            using RegistryKey key = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows NT\CurrentVersion\Fonts");

            foreach (string file in font.Files)
            {
                string destination = Path.Combine(target, file);
                File.Copy(Path.Combine(SourceFolder, file), destination, true);
                key.SetValue(Path.GetFileNameWithoutExtension(file) + " (TrueType)", destination, RegistryValueKind.String);
                AddFontResource(destination);
            }

            SendMessageTimeout(new IntPtr(0xFFFF), 0x001D, IntPtr.Zero, IntPtr.Zero, 2, 1000, out _);
        }

        [System.Runtime.InteropServices.DllImport("gdi32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
        private static extern int AddFontResource(string path);

        [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
        private static extern IntPtr SendMessageTimeout(IntPtr window, uint message, IntPtr wParam, IntPtr lParam, uint flags, uint timeout, out IntPtr result);

        public static string SettingsPath()
        {
            string forced = Environment.GetEnvironmentVariable("FINALL_TERMINAL_SETTINGS") ?? string.Empty;

            if (forced.Length > 0 && File.Exists(forced))
            {
                return forced;
            }

            if (!OperatingSystem.IsWindows())
            {
                return string.Empty;
            }

            string local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string[] candidates =
            {
                Path.Combine(local, "Packages", "Microsoft.WindowsTerminal_8wekyb3d8bbwe", "LocalState", "settings.json"),
                Path.Combine(local, "Packages", "Microsoft.WindowsTerminalPreview_8wekyb3d8bbwe", "LocalState", "settings.json"),
                Path.Combine(local, "Microsoft", "Windows Terminal", "settings.json")
            };

            return candidates.FirstOrDefault(File.Exists) ?? string.Empty;
        }

        private static JsonNode? ReadSettings(string path)
        {
            JsonDocumentOptions options = new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true };
            return JsonNode.Parse(File.ReadAllText(path), null, options);
        }

        private static string? FaceOf(JsonNode? node)
        {
            return node?["font"]?["face"]?.GetValue<string>() ?? node?["fontFace"]?.GetValue<string>();
        }

        public static string CurrentFont()
        {
            string path = SettingsPath();

            if (path.Length == 0)
            {
                return string.Empty;
            }

            try
            {
                JsonNode? root = ReadSettings(path);
                JsonNode? profile = ActiveProfile(root);

                return FaceOf(profile) ?? FaceOf(root?["profiles"]?["defaults"]) ?? "Cascadia Mono";
            }
            catch
            {
                return string.Empty;
            }
        }

        private static JsonNode? ActiveProfile(JsonNode? root)
        {
            string? id = Environment.GetEnvironmentVariable("WT_PROFILE_ID");

            if (string.IsNullOrEmpty(id))
            {
                return null;
            }

            JsonArray? list = root?["profiles"]?["list"] as JsonArray;

            return list?.FirstOrDefault(item => string.Equals(item?["guid"]?.GetValue<string>(), id, StringComparison.OrdinalIgnoreCase));
        }

        public static string CurrentFace()
        {
            return CurrentFont();
        }

        public static string CurrentMain()
        {
            return Parts(CurrentFont()).FirstOrDefault() ?? string.Empty;
        }

        public static string CurrentGeorgian()
        {
            List<string> parts = Parts(CurrentFont());

            return parts.Count > 1 ? parts[1] : FontCatalog.Fallback;
        }

        private static List<string> Parts(string face)
        {
            return face.Split(',').Select(part => part.Trim()).Where(part => part.Length > 0).ToList();
        }

        public static string Compose(string main, string georgian)
        {
            List<string> parts = new List<string> { main };

            if (georgian != main)
            {
                parts.Add(georgian);
            }

            if (!parts.Contains(FontCatalog.Fallback))
            {
                parts.Add(FontCatalog.Fallback);
            }

            return string.Join(", ", parts);
        }

        public static bool IsBold()
        {
            string path = SettingsPath();

            if (path.Length == 0)
            {
                return false;
            }

            try
            {
                JsonNode? root = ReadSettings(path);
                JsonNode? node = ActiveProfile(root)?["font"]?["weight"] ?? root?["profiles"]?["defaults"]?["font"]?["weight"];

                return node?.ToString() == "bold";
            }
            catch
            {
                return false;
            }
        }

        public static void ApplyToTerminal(string main, string georgian)
        {
            ApplySettings(Compose(main, georgian), null);
        }

        public static void ApplyBold(bool bold)
        {
            ApplySettings(null, bold);
        }

        private static void ApplySettings(string? face, bool? bold)
        {
            string path = SettingsPath();

            if (path.Length == 0)
                throw new Exceptions.NotFoundException(Translator.T("Windows Terminal settings were not found."));

            JsonNode root = ReadSettings(path) ?? new JsonObject();
            JsonObject profiles = root["profiles"] as JsonObject ?? new JsonObject();
            root["profiles"] = profiles;
            JsonObject defaults = profiles["defaults"] as JsonObject ?? new JsonObject();
            profiles["defaults"] = defaults;

            JsonNode? active = ActiveProfile(root);

            foreach (JsonObject target in new[] { defaults }.Concat(active is JsonObject activeObject ? new[] { activeObject } : Array.Empty<JsonObject>()))
            {
                if (face != null && (target == defaults || FaceOf(target) != null))
                {
                    SetFace(target, face);
                }

                if (bold != null && (target == defaults || target["font"]?["weight"] != null))
                {
                    JsonObject font = target["font"] as JsonObject ?? new JsonObject();
                    font["weight"] = bold.Value ? "bold" : "normal";
                    target["font"] = font;
                }
            }

            File.Copy(path, path + ".bak", true);
            File.WriteAllText(path, root.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
        }

        private static void SetFace(JsonObject target, string face)
        {
            target.Remove("fontFace");
            JsonObject font = target["font"] as JsonObject ?? new JsonObject();
            font["face"] = face;
            target["font"] = font;
        }
    }
}
