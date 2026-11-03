using System.Collections.Generic;
using System.Globalization;
using System.IO;

internal static class L
{
    public static string Language = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "zh" ? "zh-CN" : "en";
    static Dictionary<string, object> catalogs;
    public static void Load(string root) { catalogs = Json.Decode(File.ReadAllText(Path.Combine(root, "assets", "locales.json"))); }
    public static string T(string key)
    {
        if (catalogs == null) return key;
        return (string)((Dictionary<string, object>)catalogs[Language])[key];
    }
}
