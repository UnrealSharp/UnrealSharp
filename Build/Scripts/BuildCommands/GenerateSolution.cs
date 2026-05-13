using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using AutomationTool;
using UnrealSharp.Automation.Processes;
using UnrealSharp.Automation.Utilities;

namespace UnrealSharp.Automation.BuildCommands;

[Help("Generates a new .sln file for the project and adds all existing C# projects to it.")]
public class GenerateSolution : BuildCommand
{
    public override void ExecuteBuild()
    {
        GenerateManagedSolution(this);
    }

    public static void GenerateManagedSolution(BuildCommand buildCommand)
    {
        DotnetProcess GenerateSlnProcess = new DotnetProcess();

        string ScriptDirectory = buildCommand.GetProjectScriptFolder();

        if (!Directory.Exists(ScriptDirectory))
        {
            Directory.CreateDirectory(ScriptDirectory);
        }

        GenerateSlnProcess.StartInfo.ArgumentList.Add("new");
        GenerateSlnProcess.StartInfo.ArgumentList.Add("sln");

        GenerateSlnProcess.StartInfo.ArgumentList.Add("--format");
        GenerateSlnProcess.StartInfo.ArgumentList.Add("sln");

        string SolutionName = "Managed" + buildCommand.GetProjectName();
        string SolutionPath = Path.Combine(ScriptDirectory, $"{SolutionName}.sln");

        GenerateSlnProcess.StartInfo.ArgumentList.Add("-n");
        GenerateSlnProcess.StartInfo.ArgumentList.Add(SolutionName);
        GenerateSlnProcess.StartInfo.WorkingDirectory = ScriptDirectory;

        GenerateSlnProcess.StartInfo.ArgumentList.Add("--force");
        GenerateSlnProcess.StartBuildToolProcess();

        List<string> ExistingProjectsList = buildCommand.GetUnrealSharpProjectFiles()
            .Select(x => Path.GetRelativePath(ScriptDirectory, x.FullName))
            .ToList();

        AddProjectToSln(buildCommand.GetProjectRootFolder(), ExistingProjectsList, SolutionPath);

        DotNetSdkUtilities.CopyGlobalJson(buildCommand);
    }

    private static void AddProjectToSln(string projectDirectory, List<string> relativePaths, string solutionPath)
    {
        string SolutionDirectory = Path.GetDirectoryName(solutionPath)!;
        IEnumerable<IGrouping<string, string>> GroupedProjects = GroupPathsBySolutionFolder(SolutionDirectory, projectDirectory, relativePaths);
        
        foreach (IGrouping<string, string> Projects in GroupedProjects)
        {
            bool Unlocked = WaitForFileUnlock(solutionPath, 10000, 200);
            if (!Unlocked)
            {
                Console.WriteLine($"Warning: timed out waiting for {solutionPath} to become available. Will still try to add projects.");
            }

            const int maxAttempts = 10;
            int Attempt = 0;
            bool Success = false;

            while (Attempt < maxAttempts && !Success)
            {
                Attempt++;
                try
                {
                    DotnetProcess AddProjectProcess = new DotnetProcess();
                    AddProjectProcess.StartInfo.ArgumentList.Add("sln");
                    AddProjectProcess.StartInfo.ArgumentList.Add("add");
                    AddProjectProcess.StartInfo.ArgumentList.Add("--include-references");
                    AddProjectProcess.StartInfo.ArgumentList.Add("false");

                    foreach (string RelativePath in Projects)
                    {
                        AddProjectProcess.StartInfo.ArgumentList.Add(RelativePath);
                    }

                    AddProjectProcess.StartInfo.ArgumentList.Add("-s");
                    AddProjectProcess.StartInfo.ArgumentList.Add(Projects.Key);
                    AddProjectProcess.StartInfo.WorkingDirectory = SolutionDirectory;

                    AddProjectProcess.StartBuildToolProcess();
                    Success = true;
                }
                catch (IOException Ex)
                {
                    Console.WriteLine($"Attempt {Attempt}/{maxAttempts}: IOException while adding projects to sln: {Ex.Message}. Retrying...");
                    Thread.Sleep(250 * Attempt);
                }
                catch (Exception Ex)
                {
                    Console.WriteLine($"Attempt {Attempt}/{maxAttempts}: error while adding projects to sln: {Ex.Message}. Retrying...");
                    Thread.Sleep(250 * Attempt);
                }
            }

            if (!Success)
            {
                throw new Exception($"Failed to add projects to solution '{solutionPath}' after {maxAttempts} attempts.");
            }
        }
    }

    private static bool WaitForFileUnlock(string filePath, int timeoutMs = 10000, int pollIntervalMs = 150)
    {
        Stopwatch Stopwatch = Stopwatch.StartNew();

        while (Stopwatch.ElapsedMilliseconds < timeoutMs)
        {
            if (File.Exists(filePath))
            {
                break;
            }

            Thread.Sleep(pollIntervalMs);
        }

        if (!File.Exists(filePath))
        {
            return false;
        }

        while (Stopwatch.ElapsedMilliseconds < timeoutMs)
        {
            try
            {
                using (new FileStream(filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
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

    private static IEnumerable<IGrouping<string, string>> GroupPathsBySolutionFolder(string scriptDirectory, string projectDirectory, List<string> relativePaths)
    {
        return relativePaths.GroupBy(path => GetPathRelativeToProject(scriptDirectory, projectDirectory, path));
    }

    private static string GetPathRelativeToProject(string scriptDirectory, string projectDirectory, string relativePath)
    {
        string ScriptDirectoryName = Path.GetDirectoryName(scriptDirectory)!;
        
        string FullPath = Path.GetFullPath(relativePath, scriptDirectory);
        string ProjectRelativePath = Path.GetRelativePath(projectDirectory, FullPath);
        string ProjectDirName = Path.GetDirectoryName(ProjectRelativePath)!;
        
        string ContainingDirName = Path.GetDirectoryName(ProjectDirName)!;
        return ContainingDirName == ScriptDirectoryName ? ContainingDirName : Path.GetDirectoryName(ContainingDirName)!;
    }
}
