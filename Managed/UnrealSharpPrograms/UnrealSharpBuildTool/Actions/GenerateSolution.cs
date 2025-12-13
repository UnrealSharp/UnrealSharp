using System.Diagnostics;

namespace UnrealSharpBuildTool.Actions;

public class GenerateSolution : BuildToolAction
{
    public override bool RunAction()
    {
        using BuildToolProcess generateSln = new BuildToolProcess();

        generateSln.StartInfo.ArgumentList.Add("new");
        generateSln.StartInfo.ArgumentList.Add("sln");

        generateSln.StartInfo.ArgumentList.Add("-n");
        generateSln.StartInfo.ArgumentList.Add(Program.GetProjectNameAsManaged());
        generateSln.StartInfo.WorkingDirectory = Program.GetScriptFolder();

        generateSln.StartInfo.ArgumentList.Add("--force");
        generateSln.StartBuildToolProcess();

        List<string> existingProjectsList = GetExistingProjects()
            .Select(x => Path.GetRelativePath(Program.GetScriptFolder(), x))
            .ToList();

        AddProjectToSln(existingProjectsList);
        CopyGlobalJson();
        return true;
    }

    private static IEnumerable<string> GetExistingProjects()
    {
        DirectoryInfo scriptsDirectory = new DirectoryInfo(Program.GetScriptFolder());
        DirectoryInfo pluginsDirectory = new DirectoryInfo(Program.GetPluginsFolder());
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

    private static void AddProjectToSln(List<string> relativePaths)
    {
        string slnPath = Path.Combine(Program.GetScriptFolder(), $"{Program.GetProjectNameAsManaged()}.sln");

        foreach (IGrouping<string, string> projects in GroupPathsBySolutionFolder(relativePaths))
        {
            bool unlocked = WaitForFileUnlock(slnPath, 10000, 200);
            if (!unlocked)
            {
                Console.WriteLine($"Warning: timed out waiting for {slnPath} to become available. Will still try to add projects.");
            }
            
            const int maxAttempts = 10;
            int attempt = 0;
            bool success = false;

            while (attempt < maxAttempts && !success)
            {
                attempt++;
                try
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
                    success = true;
                }
                catch (IOException ex)
                {
                    Console.WriteLine($"Attempt {attempt}/{maxAttempts}: IOException while adding projects to sln: {ex.Message}. Retrying...");
                    Thread.Sleep(250 * attempt);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Attempt {attempt}/{maxAttempts}: error while adding projects to sln: {ex.Message}. Retrying...");
                    Thread.Sleep(250 * attempt);
                }
            }

            if (!success)
            {
                throw new Exception($"Failed to add projects to solution '{slnPath}' after {maxAttempts} attempts.");
            }
        }
    }

    private static bool WaitForFileUnlock(string path, int timeoutMs = 10000, int pollIntervalMs = 150)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();

        while (stopwatch.ElapsedMilliseconds < timeoutMs)
        {
            if (File.Exists(path))
            {
                break;
            }

            Thread.Sleep(pollIntervalMs);
        }

        if (!File.Exists(path))
        {
            return false;
        }

        while (stopwatch.ElapsedMilliseconds < timeoutMs)
        {
            try
            {
                using (new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                {
                    return true;
                }
            }
            catch (IOException)
            {
                Thread.Sleep(pollIntervalMs);
            }
            catch (UnauthorizedAccessException)
            {
                Thread.Sleep(pollIntervalMs);
            }
        }
        
        return false;
    }

    private static IEnumerable<IGrouping<string, string>> GroupPathsBySolutionFolder(List<string> relativePaths)
    {
        return relativePaths.GroupBy(GetPathRelativeToProject)!;
    }

    private static string GetPathRelativeToProject(string path)
    {
        string fullPath = Path.GetFullPath(path, Program.GetScriptFolder());
        string relativePath = Path.GetRelativePath(Program.GetProjectDirectory(), fullPath);
        string projectDirName = Path.GetDirectoryName(relativePath)!;

        // If we're in the script folder we want these to be in the Script solution folder, otherwise we want these to
        // be in the directory for the plugin itself.
        string containingDirName = Path.GetDirectoryName(projectDirName)!;
        return containingDirName == "Script" ? containingDirName : Path.GetDirectoryName(containingDirName)!;
    }
    
    private static void CopyGlobalJson()
    {
        string sourceGlobalJsonPath = Path.Combine(Program.BuildToolOptions.PluginDirectory, "Managed", "global.json");
        string destinationGlobalJsonPath = Path.Combine(Program.GetScriptFolder(), "global.json");
        File.Copy(sourceGlobalJsonPath, destinationGlobalJsonPath, overwrite: true);
    }
}