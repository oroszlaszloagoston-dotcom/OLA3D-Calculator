using System.Text.Json;

namespace OLA3D.Calculator;

using static OLA3D.Calculator.Localizer;

public sealed class AppSettings
{
    public DesignPricingSettings Design { get; set; } = new();
    public PrintPricingSettings Print { get; set; } = new();
    public List<MaterialDefinition> Materials { get; set; } = MaterialDefinition.CreateDefaults();

    public static AppSettings CreateDefaults()
    {
        var settings = new AppSettings();
        if (SettingsStore.Currency == "HUF") return settings;
        // Suggested independent price lists, not live foreign-exchange rates.
        decimal scale = SettingsStore.Currency == "EUR" ? 0.0026m : 0.003m;
        settings.Design.HourlyRate = SettingsStore.Currency == "EUR" ? 13m : 15m;
        settings.Design.MinimumFee = Math.Ceiling(settings.Design.MinimumFee * scale);
        settings.Design.TravelBaseFee *= scale;
        settings.Design.TravelPerKm = decimal.Round(settings.Design.TravelPerKm * scale, 2);
        settings.Design.RoundingStep = 1m;
        settings.Print.MachineHourlyRate *= scale;
        settings.Print.MinimumFee = Math.Ceiling(settings.Print.MinimumFee * scale);
        settings.Print.RoundingStep = 1m;
        foreach (var material in settings.Materials)
            material.PricePerKg = decimal.Round(material.PricePerKg * scale, 2);
        return settings;
    }
}

public sealed class DesignPricingSettings
{
    public decimal HourlyRate { get; set; } = 5000m;
    public decimal MinimumFee { get; set; } = 8000m;

    public decimal BaseVerySimpleHours { get; set; } = 0.75m;
    public decimal BaseSimpleHours { get; set; } = 1.25m;
    public decimal BaseMediumHours { get; set; } = 2.50m;
    public decimal BaseComplexHours { get; set; } = 4.00m;
    public decimal BaseVeryComplexHours { get; set; } = 6.00m;

    public decimal FunctionDecorationFactor { get; set; } = 0.90m;
    public decimal FunctionCoverFactor { get; set; } = 1.00m;
    public decimal FunctionFunctionalFactor { get; set; } = 1.20m;
    public decimal FunctionLoadedFactor { get; set; } = 1.40m;

    public decimal SourceDrawingFactor { get; set; } = 0.85m;
    public decimal SourcePhysicalFactor { get; set; } = 1.00m;
    public decimal SourceMeshFactor { get; set; } = 1.15m;
    public decimal SourcePhotoFactor { get; set; } = 1.35m;

    public decimal PrecisionNormalFactor { get; set; } = 1.00m;
    public decimal PrecisionFitFactor { get; set; } = 1.15m;
    public decimal PrecisionHighFactor { get; set; } = 1.30m;

    public decimal Interface0Factor { get; set; } = 1.00m;
    public decimal Interface1Factor { get; set; } = 1.08m;
    public decimal Interface2Factor { get; set; } = 1.16m;
    public decimal Interface3Factor { get; set; } = 1.24m;
    public decimal Interface4Factor { get; set; } = 1.32m;
    public decimal Interface5PlusFactor { get; set; } = 1.40m;

    public decimal ScanFirstHours { get; set; } = 0.75m;
    public decimal ScanAdditionalHours { get; set; } = 0.40m;
    public decimal ScanNormalFactor { get; set; } = 1.00m;
    public decimal ScanDifficultSurfaceFactor { get; set; } = 1.20m;
    public decimal ScanComplexFactor { get; set; } = 1.40m;

    public decimal RevisionHours { get; set; } = 0.40m;
    public decimal DrawingHours { get; set; } = 0.75m;
    public decimal DfmHours { get; set; } = 0.40m;

    public decimal RushNormalFactor { get; set; } = 1.00m;
    public decimal Rush48Factor { get; set; } = 1.25m;
    public decimal Rush24Factor { get; set; } = 1.50m;

    public decimal TravelBaseFee { get; set; } = 5000m;
    public decimal TravelPerKm { get; set; } = 200m;

    public decimal BufferDefinedFactor { get; set; } = 1.00m;
    public decimal BufferNormalFactor { get; set; } = 1.10m;
    public decimal BufferUncertainFactor { get; set; } = 1.15m;
    public decimal BufferHighRiskFactor { get; set; } = 1.20m;

    public decimal QuoteLowFactor { get; set; } = 0.95m;
    public decimal QuoteHighFactor { get; set; } = 1.10m;
    public decimal RoundingStep { get; set; } = 500m;
}

public sealed class PrintPricingSettings
{
    public decimal MachineHourlyRate { get; set; } = 1000m;
    public decimal MinimumFee { get; set; } = 3000m;
    public decimal WasteFactor { get; set; } = 1.10m;
    public decimal QuoteLowFactor { get; set; } = 0.95m;
    public decimal QuoteHighFactor { get; set; } = 1.10m;
    public decimal RoundingStep { get; set; } = 500m;
}

public sealed class MaterialDefinition
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = T("Új anyag", "New material");
    public decimal PricePerKg { get; set; } = 8000m;
    public decimal Factor { get; set; } = 1.00m;

    public static List<MaterialDefinition> CreateDefaults() =>
    [
        new() { Name = "PLA", PricePerKg = 7000m, Factor = 1.00m },
        new() { Name = "PETG", PricePerKg = 8000m, Factor = 1.05m },
        new() { Name = "ABS", PricePerKg = 8000m, Factor = 1.10m },
        new() { Name = "ASA", PricePerKg = 9500m, Factor = 1.15m },
        new() { Name = "TPU", PricePerKg = 11000m, Factor = 1.15m },
        new() { Name = "PA / Nylon", PricePerKg = 15000m, Factor = 1.25m },
        new() { Name = "PC", PricePerKg = 14000m, Factor = 1.25m },
        new() { Name = "PPS", PricePerKg = 30000m, Factor = 1.35m },
        new() { Name = "Support PLA/PETG", PricePerKg = 16000m, Factor = 1.10m }
    ];
}

public static class SettingsStore
{
    internal static string? DirectoryOverride { get; set; }
    public static string Currency { get; private set; } = "HUF";
    public static string SettingsDirectory => DirectoryOverride ?? Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "OLA3D Calculator");

    public static string SettingsPath => Path.Combine(SettingsDirectory, $"settings-{Currency}.json");

    public static void InitializeCurrency()
    {
        string? code = InstallationOptions.Read("Currency");
        Currency = code is "EUR" or "USD" ? code : "HUF";
    }

    public static AppSettings Load()
    {
        try
        {
            if (!File.Exists(SettingsPath))
                return AppSettings.CreateDefaults();

            var json = File.ReadAllText(SettingsPath);
            var loaded = JsonSerializer.Deserialize<AppSettings>(json);
            if (loaded is null)
                return AppSettings.CreateDefaults();

            var defaults = AppSettings.CreateDefaults();
            loaded.Design ??= defaults.Design;
            loaded.Print ??= defaults.Print;
            if (loaded.Materials is null || loaded.Materials.Count == 0)
                loaded.Materials = defaults.Materials;

            foreach (var material in loaded.Materials)
            {
                if (string.IsNullOrWhiteSpace(material.Id))
                    material.Id = Guid.NewGuid().ToString("N");
                if (string.IsNullOrWhiteSpace(material.Name))
                    material.Name = T("Névtelen anyag", "Unnamed material");
            }

            return loaded;
        }
        catch
        {
            return AppSettings.CreateDefaults();
        }
    }

    public static void Save(AppSettings settings)
    {
        Directory.CreateDirectory(SettingsDirectory);
        var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(SettingsPath, json);
    }

    public static AppSettings ResetToFactoryDefaults()
    {
        var defaults = AppSettings.CreateDefaults();
        Save(defaults);
        return defaults;
    }
}
