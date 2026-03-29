namespace Ecommerce.API;

internal static class DotEnvBootstrap
{
    internal static void LoadIfPresent()
    {
        foreach (var path in GetCandidatePaths())
        {
            if (File.Exists(path))
            {
                DotNetEnv.Env.Load(path);
                return;
            }
        }
    }

    private static IEnumerable<string> GetCandidatePaths()
    {
        yield return Path.Combine(Directory.GetCurrentDirectory(), ".env");
        yield return Path.Combine(AppContext.BaseDirectory, ".env");
        var fromBin = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".env"));
        yield return fromBin;
    }
}
