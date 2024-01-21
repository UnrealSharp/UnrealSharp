namespace UnrealSharpBuildTool;
public static class DotNetPathFinder
{ 
    public static string? FindDotNetExecutable()
    {
        var pathVariable = Environment.GetEnvironmentVariable("PATH");
        
        if (pathVariable == null)
        {
            return null;
        }
        
        var paths = pathVariable.Split(Path.PathSeparator);
        
        foreach (var path in paths)
        {
            var fullPath = Path.Combine(path, "dotnet.exe");
            if (File.Exists(fullPath))
            {
                return fullPath;
            }
        }

        throw new Exception("Couldn't find dotnet.exe!");
    }
}