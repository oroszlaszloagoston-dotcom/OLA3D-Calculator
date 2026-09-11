namespace OLA3D.Calculator;

internal static class InstallationOptions
{
    internal static string? DirectoryOverride { get; set; }

    internal static string? Read(string key)
    {
        try
        {
            string path = Path.Combine(DirectoryOverride ?? AppContext.BaseDirectory, "language.ini");
            return File.Exists(path)
                ? File.ReadLines(path).FirstOrDefault(line => line.StartsWith(key + "=", StringComparison.OrdinalIgnoreCase))?.Split('=', 2)[1].Trim()
                : null;
        }
        catch (IOException) { return null; }
        catch (UnauthorizedAccessException) { return null; }
    }
}
