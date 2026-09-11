namespace OLA3D.Calculator;

internal static class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        Localizer.Initialize(args);
        SettingsStore.InitializeCurrency();
        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm());
    }
}
