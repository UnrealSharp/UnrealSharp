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

        AddProjectToSln(existingProjectsList);
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
        IEnumerable<FileInfo> files = directoryInfo.EnumerateFiles("*.csproj", SearchOption.AllDirectories);
        return files;
    }
    
    public static void AddProjectToSln(string relativePath)
    {
        AddProjectToSln([relativePath]);
    }

    public static void AddProjectToSln(List<string> relativePaths)
    {
        foreach (IGrouping<string, string> projects in GroupPathsBySolutionFolder(relativePaths))
        {
            using BuildToolProcess addProjectToSln = new BuildToolProcess();
            addProjectToSln.StartInfo.ArgumentList.Add("sln");
            addProjectToSln.StartInfo.ArgumentList.Add("add");

            foreach (string relativePath in projects)
            {
                addProjectToSln.StartInfo.ArgumentList.Add(relativePath);
            }

            addProjectToSln.StartInfo.ArgumentList.Add("-s");
            addProjectToSln.StartInfo.ArgumentList.Add(projects.Key);

            addProjectToSln.StartInfo.WorkingDirectory = Program.GetScriptFolder();
            addProjectToSln.StartBuildToolProcess();
        }
    }
    
    private static IEnumerable<IGrouping<string, string>> GroupPathsBySolutionFolder(List<string> relativePaths)
    {
        return relativePaths.GroupBy(GetPathRelativeToProject)!;
    }

    private static string GetPathRelativeToProject(string path)
    {
        var fullPath = Path.GetFullPath(path, Program.GetScriptFolder());
        var relativePath = Path.GetRelativePath(Program.GetProjectDirectory(), fullPath);
        var projectDirName = Path.GetDirectoryName(relativePath)!;

        // If we're in the script folder we want these to be in the Script solution folder, otherwise we want these to
        // be in the directory for the plugin itself.
        var containingDirName = Path.GetDirectoryName(projectDirName)!;
        return containingDirName == "Script" ? containingDirName : Path.GetDirectoryName(containingDirName)!;
    }
}
