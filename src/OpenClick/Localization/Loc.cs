using System.Globalization;

namespace OpenClick.Localization;

/// <summary>Language the user asked for. <see cref="System"/> follows the Windows UI language.</summary>
public enum AppLanguage
{
    System,
    English,
    Chinese,
}

/// <summary>
/// Runtime UI language and the lookup used for every user-visible string. Translations live in
/// <see cref="Strings"/>; the UI code only references keys, so adding a language means adding one
/// column to the table and one enum value.
/// </summary>
public static class Loc
{
    private static readonly CultureInfo EnglishCulture = CultureInfo.GetCultureInfo("en-US");
    private static readonly CultureInfo ChineseCulture = CultureInfo.GetCultureInfo("zh-CN");

    private static AppLanguage _requested = AppLanguage.System;
    private static AppLanguage _effective = AppLanguage.English;

    /// <summary>Raised after the effective language changes so an open window can re-apply its texts.</summary>
    public static event Action? Changed;

    public static AppLanguage Requested => _requested;

    public static AppLanguage Effective => _effective;

    public static bool IsChinese => _effective == AppLanguage.Chinese;

    /// <summary>Chinese text renders better in a CJK face; Latin text keeps the original Segoe UI.</summary>
    public static string UiFontFamily => IsChinese ? "Microsoft YaHei UI" : "Segoe UI";

    public static CultureInfo Culture => IsChinese ? ChineseCulture : EnglishCulture;

    /// <summary>Applies a language without raising <see cref="Changed"/>; used before the UI exists.</summary>
    public static void Initialize(AppLanguage language) => Apply(language, notify: false);

    /// <summary>Applies a language and notifies an already-built UI.</summary>
    public static void SetLanguage(AppLanguage language) => Apply(language, notify: true);

    /// <summary>Returns the string for <paramref name="key"/> in the current language.</summary>
    public static string T(string key) =>
        Strings.Table.TryGetValue(key, out (string English, string Chinese) entry)
            ? (IsChinese ? entry.Chinese : entry.English)
            : key;

    /// <summary>Formats the string for <paramref name="key"/> with the current language's culture.</summary>
    public static string F(string key, params object?[] args) => string.Format(Culture, T(key), args);

    private static void Apply(AppLanguage language, bool notify)
    {
        _requested = language;
        AppLanguage effective = Resolve(language);
        bool changed = effective != _effective;
        _effective = effective;

        CultureInfo culture = Culture;
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;

        if (notify && changed)
        {
            Changed?.Invoke();
        }
    }

    private static AppLanguage Resolve(AppLanguage language) => language switch
    {
        AppLanguage.Chinese => AppLanguage.Chinese,
        AppLanguage.English => AppLanguage.English,
        _ => DetectOsLanguage(),
    };

    private static AppLanguage DetectOsLanguage()
    {
        string name;
        try
        {
            name = CultureInfo.InstalledUICulture.TwoLetterISOLanguageName;
        }
        catch (Exception)
        {
            name = "en";
        }

        return string.Equals(name, "zh", StringComparison.OrdinalIgnoreCase)
            ? AppLanguage.Chinese
            : AppLanguage.English;
    }
}
