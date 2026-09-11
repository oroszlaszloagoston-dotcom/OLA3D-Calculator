using System.Globalization;
using System.Reflection;
using OLA3D.Calculator;

internal static class Program
{
    private static int assertions;
    private static T Field<T>(MainForm form, string name) => (T)typeof(MainForm).GetField(name, BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(form)!;
    private static object? Invoke(MainForm form, string name, params object[] args) => typeof(MainForm).GetMethod(name, BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(form, args);
    private static void Check(bool condition, string message)
    {
        assertions++;
        if (!condition) throw new Exception(message);
    }

    private static string installationDirectory = "";
    private static void InstallOptions(string currency, string language = "en")
    {
        Directory.CreateDirectory(installationDirectory);
        File.WriteAllText(Path.Combine(installationDirectory, "language.ini"), $"[Application]\nLanguage={language}\nCurrency={currency}\n");
        Localizer.Initialize([]);
        SettingsStore.InitializeCurrency();
    }

    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        string testDirectory = Path.Combine(Path.GetTempPath(), "OLA3D-tests-" + Guid.NewGuid().ToString("N"));
        typeof(SettingsStore).GetProperty("DirectoryOverride", BindingFlags.NonPublic | BindingFlags.Static)!.SetValue(null, testDirectory);
        installationDirectory = Path.Combine(testDirectory, "installation");
        typeof(Localizer).Assembly.GetType("OLA3D.Calculator.InstallationOptions")!.GetProperty("DirectoryOverride", BindingFlags.NonPublic | BindingFlags.Static)!.SetValue(null, installationDirectory);
        try
        {
            foreach (string language in new[] { "hu", "en" })
            {
                Localizer.Initialize(["--language=" + language]);
                foreach (var (currency, hourly, designPrice, printPrice) in new[] { ("HUF", 5000m, 20500m, 11000m), ("EUR", 13m, 54m, 29m), ("USD", 15m, 62m, 33m) })
                {
                    InstallOptions(currency, language);
                    Check(Localizer.Language == language, "Installed language is used");
                    SettingsStore.ResetToFactoryDefaults();
                    using var form = new MainForm();
                    Check(SettingsStore.Load().Design.HourlyRate == hourly, $"Default rate {currency}");
                    string suffix = Localizer.MoneyUnit;
                    string Expected(decimal amount) => amount.ToString(currency == "HUF" ? "N0" : "N2", Localizer.Culture) + " " + suffix;
                    Check(Field<Label>(form, "lblDesignFinal").Text == Expected(designPrice), $"Design total {language}/{currency}");
                    Field<NumericUpDown>(form, "nudPrintHours").Value = 10;
                    var grid = Field<DataGridView>(form, "dgvPrintMaterials");
                    grid.Rows[0].Cells["Grams"].Value = "100";
                    Invoke(form, "RecalculatePrint");
                    Check(Field<Label>(form, "lblPrintFinal").Text == Expected(printPrice), $"Print total {language}/{currency}");
                    Check(Field<ComboBox>(form, "cmbBaseHours").Items[0]?.ToString() == (language == "hu" ? "Nagyon egyszerű" : "Very simple"), "Translated options");
                    Check((bool)Invoke(form, "SaveSettingsFromUi")!, "Settings save");
                    Check(SettingsStore.Load().Design.HourlyRate == hourly, "Hourly rate survives UI save without clamping");
                    Check(SettingsStore.Load().Materials[3].PricePerKg == AppSettings.CreateDefaults().Materials[3].PricePerKg, "Material decimal prices survive UI save");
                    var parse = typeof(MainForm).GetMethod("ParseDecimal", BindingFlags.NonPublic | BindingFlags.Static)!;
                    Check((decimal)parse.Invoke(null, ["1,25"])! == 1.25m, "Comma decimal");
                    Check((decimal)parse.Invoke(null, ["1.25"])! == 1.25m, "Dot decimal");
                    Check(typeof(MainForm).GetField("cmbCurrency", BindingFlags.NonPublic | BindingFlags.Instance) is null, "No runtime currency selector");
                }
            }
            InstallOptions("EUR");
            var custom = SettingsStore.Load();
            custom.Design.HourlyRate = 37.5m;
            SettingsStore.Save(custom);
            InstallOptions("HUF");
            Check(SettingsStore.Load().Design.HourlyRate == 5000m, "Currency profiles are independent");
            InstallOptions("EUR");
            Check(SettingsStore.Load().Design.HourlyRate == 37.5m, "Custom EUR rate is retained");
            SettingsStore.ResetToFactoryDefaults();
            Check(SettingsStore.Load().Design.HourlyRate == 13m, "Factory reset uses selected currency");
            InstallOptions("invalid");
            Check(SettingsStore.Currency == "HUF", "Invalid installation currency safely defaults to HUF");
            Console.WriteLine($"PASS: {assertions} assertions across both languages and all three currencies.");
        }
        finally
        {
            if (Directory.Exists(testDirectory)) Directory.Delete(testDirectory, true);
        }
    }
}
