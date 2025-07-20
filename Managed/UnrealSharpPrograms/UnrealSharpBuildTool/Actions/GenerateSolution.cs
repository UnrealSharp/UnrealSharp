namespace UnrealSharpBuildTool.Actions;

public class GenerateSolution : BuildToolAction
{
    public override bool RunAction()
    {
        using BuildToolProcess generateSln = new BuildToolProcess();

        // Create a solution.
        generateSln.StartInfo.ArgumentList.Add("new");
        generateSln.StartInfo.ArgumentList.Add("sln");

        // Assign project name to the solution.
        generateSln.StartInfo.ArgumentList.Add("-n");
        generateSln.StartInfo.ArgumentList.Add(Program.GetProjectNameAsManaged());
        generateSln.StartInfo.WorkingDirectory = Program.GetScriptFolder();

        // Force the creation of the solution.
        generateSln.StartInfo.ArgumentList.Add("--force");
        generateSln.StartBuildToolProcess();

        List<string> existingProjectsList = GetExistingProjects()
                .Select(x => Path.GetRelativePath(Program.GetScriptFolder(), x))
                .ToList();

        GenerateProject.AddProjectToSln(existingProjectsList);
        return true;
    }

    private static IEnumerable<string> GetExistingProjects()
    {
        var scriptsDirectory = new DirectoryInfo(Program.GetScriptFolder());
        var pluginsDirectory = new DirectoryInfo(Program.GetPluginsFolder());
        return FindCSharpProjects(scriptsDirectory)
                .Concat(pluginsDirectory.EnumerateFiles("*.uplugin", SearchOption.AllDirectories)
                        .Select(x => x.Directory)
                        .SelectMany(x => x!.EnumerateDirectories("Script"))
                        .SelectMany(FindCSharpProjects))
                .Select(x => x.FullName);
    }

    private static IEnumerable<FileInfo> FindCSharpProjects(DirectoryInfo directoryInfo)
    {
        return directoryInfo.EnumerateFiles("*.csproj", SearchOption.AllDirectories);
    }
}
