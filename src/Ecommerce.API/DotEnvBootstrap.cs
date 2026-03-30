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

    /// <summary>
    /// Tìm file .env: cùng cấp với .sln (thư mục chạy dotnet), hoặc đi ngược từ BaseDirectory / cwd.
    /// </summary>
    private static IEnumerable<string> GetCandidatePaths()
    {
        foreach (var root in GetSearchRoots())
        {
            if (string.IsNullOrWhiteSpace(root) || !Directory.Exists(root))
                continue;

            var dir = new DirectoryInfo(root);
            for (var depth = 0; depth < 12 && dir != null; depth++)
            {
                yield return Path.Combine(dir.FullName, ".env");
                dir = dir.Parent;
            }
        }
    }

    private static IEnumerable<string> GetSearchRoots()
    {
        yield return Directory.GetCurrentDirectory();
        yield return AppContext.BaseDirectory;
    }
}
