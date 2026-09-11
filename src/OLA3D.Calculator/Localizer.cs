using System.Globalization;

namespace OLA3D.Calculator;

public static class Localizer
{
    public static string Language { get; private set; } = "hu";
    public static CultureInfo Culture => CultureInfo.GetCultureInfo(Language == "en" ? "en-GB" : "hu-HU");
    public static string MoneyUnit => SettingsStore.Currency == "HUF" && Language == "hu" ? "Ft" : SettingsStore.Currency;
    public static string T(string hu, string en)
    {
        string text = Language == "en" ? en : hu;
        return text.Replace("Ft", MoneyUnit).Replace("HUF", MoneyUnit);
    }

    public static void Initialize(string[] args)
    {
        string? requested = args.FirstOrDefault(a => a.StartsWith("--language=", StringComparison.OrdinalIgnoreCase))?.Split('=', 2)[1];
        if (requested is not ("hu" or "en"))
            requested = InstallationOptions.Read("Language");
        Language = requested is "hu" or "en" ? requested :
            (CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "hu" ? "hu" : "en");
        CultureInfo.CurrentCulture = Culture;
        CultureInfo.CurrentUICulture = Culture;
        CultureInfo.DefaultThreadCurrentCulture = Culture;
        CultureInfo.DefaultThreadCurrentUICulture = Culture;
    }
}
