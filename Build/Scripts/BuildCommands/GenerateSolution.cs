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
[Help("SearchFolders=<Path>+<Path>", "The list of folders to search for .csproj files to add to the solution. Paths are relative to the plugin/project root folder.")]
[Help("ProjectPaths=<Path>+<Path>", "The list of individual .csproj file paths to add to the solution. Paths are relative to the plugin/project root folder.")]
[Help("OutputFolder=<Path>", "The folder to output the generated solution file to. Defaults to the project script folder if not specified.")]
[Help("SolutionName=<Name>", "The name of the generated solution file, without the .sln extension. Defaults to the project name.")]
public class GenerateSolution : BuildCommand
{
    private const int MaxAddProjectAttempts = 10;
    private const int FileUnlockTimeoutMs = 10_000;
    private const int FileUnlockPollIntervalMs = 200;
    private const int RetryBackoffMs = 250;

    public override void ExecuteBuild()
    {
        GenerateManagedSolution(this);
    }

    public static void GenerateManagedSolution(BuildCommand buildCommand)
    {
        ArgumentNullException.ThrowIfNull(buildCommand);

        string[] SearchFolders = buildCommand.ParseParamValues("SearchFolders");
        string SolutionName = buildCommand.ParseParamValue("SolutionName", "Managed" + buildCommand.GetProjectName());
        string OutputFolder = buildCommand.ParseParamValue("OutputFolder", buildCommand.GetProjectScriptFolder());
        string SolutionPath = Path.Combine(OutputFolder, $"{SolutionName}.sln");
        
        if (!Directory.Exists(OutputFolder))
        {
            Directory.CreateDirectory(OutputFolder);
        }
        
        LoggerUtilities.LogUnrealSharpInfo($"Generating solution '{SolutionName}.sln' in '{OutputFolder}'...");

        DotnetProcess GenerateSlnProcess = new DotnetProcess();
        GenerateSlnProcess.StartInfo.ArgumentList.Add("new");
        GenerateSlnProcess.StartInfo.ArgumentList.Add("sln");
        GenerateSlnProcess.StartInfo.ArgumentList.Add("--format");
        GenerateSlnProcess.StartInfo.ArgumentList.Add("sln");
        GenerateSlnProcess.StartInfo.ArgumentList.Add("-n");
        GenerateSlnProcess.StartInfo.ArgumentList.Add(SolutionName);
        GenerateSlnProcess.StartInfo.ArgumentList.Add("--force");
        GenerateSlnProcess.StartInfo.WorkingDirectory = OutputFolder;
        GenerateSlnProcess.StartProcess();

        List<string> Projects = new List<string>();
        Projects.AddRange(buildCommand.ParseParamValues("ProjectPaths"));
        
        foreach (string SearchFolder in SearchFolders)
        {
            List<string> FoundProjects = buildCommand.GetUnrealSharpProjectFiles(SearchFolder)
                .Select(projectFile => Path.GetRelativePath(OutputFolder, projectFile.FullName)).ToList();
            
            Projects.AddRange(FoundProjects);
        }

        AddProjectsToSln(buildCommand.GetProjectRootFolder(), Projects, SolutionPath);

        DotNetSdkUtilities.CopyGlobalJson(buildCommand);
    }

    private static void AddProjectsToSln(string projectDirectory, List<string> relativePaths, string solutionPath)
    {
        string SolutionDirectory = Path.GetDirectoryName(solutionPath)!;

        IEnumerable<IGrouping<string, string>> GroupedProjects = GroupPathsBySolutionFolder(SolutionDirectory, projectDirectory, relativePaths);

        foreach (IGrouping<string, string> Projects in GroupedProjects)
        {
            if (!WaitForFileUnlock(solutionPath, FileUnlockTimeoutMs, FileUnlockPollIntervalMs))
            {
                throw new AutomationException($"Timed out waiting for solution file '{solutionPath}' to be unlocked for editing.");
            }

            RunAddProjectsWithRetry(SolutionDirectory, Projects, solutionPath);
        }
    }

    private static void RunAddProjectsWithRetry(string solutionDirectory, IGrouping<string, string> projects, string solutionPath)
    {
        for (int Attempt = 1; Attempt <= MaxAddProjectAttempts; Attempt++)
        {
            try
            {
                DotnetProcess AddProjectProcess = new DotnetProcess();
                AddProjectProcess.StartInfo.ArgumentList.Add("sln");
                AddProjectProcess.StartInfo.ArgumentList.Add("add");
                AddProjectProcess.StartInfo.ArgumentList.Add("--include-references");
                AddProjectProcess.StartInfo.ArgumentList.Add("false");

                foreach (string RelativePath in projects)
                {
                    AddProjectProcess.StartInfo.ArgumentList.Add(RelativePath);
                }

                AddProjectProcess.StartInfo.ArgumentList.Add("-s");
                AddProjectProcess.StartInfo.ArgumentList.Add(projects.Key);
                AddProjectProcess.StartInfo.WorkingDirectory = solutionDirectory;

                AddProjectProcess.StartProcess();
                return;
            }
            catch (Exception Ex)
            {
                LoggerUtilities.LogUnrealSharpWarning($"Attempt {Attempt} to add projects to solution failed: {Ex.Message}");

                if (Attempt == MaxAddProjectAttempts)
                {
                    break;
                }

                Thread.Sleep(RetryBackoffMs);
            }
        }

        throw new AutomationException($"Failed to add projects to solution '{solutionPath}' after {MaxAddProjectAttempts} attempts.");
    }

    private static bool WaitForFileUnlock(string filePath, int timeoutMs, int pollIntervalMs)
    {
        Stopwatch Stopwatch = Stopwatch.StartNew();

        while (Stopwatch.ElapsedMilliseconds < timeoutMs && !File.Exists(filePath))
        {
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
                using FileStream Stream = new FileStream(filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
                return true;
            }
            catch (IOException)
            {
                Thread.Sleep(pollIntervalMs);
            }
        }

        return false;
    }

    private static IEnumerable<IGrouping<string, string>> GroupPathsBySolutionFolder(string solutionDirectory, string projectDirectory, List<string> relativePaths)
    {
        return relativePaths.GroupBy(path => GetSolutionFolderForProject(solutionDirectory, projectDirectory, path));
    }
    
    private static string GetSolutionFolderForProject(string solutionDirectory, string projectDirectory, string relativePath)
    {
        string SolutionParentDirectory = Path.GetDirectoryName(solutionDirectory)!;

        string FullPath = Path.GetFullPath(relativePath, solutionDirectory);
        string ProjectRelativePath = Path.GetRelativePath(projectDirectory, FullPath);
        string ProjectDirName = Path.GetDirectoryName(ProjectRelativePath)!;

        string ContainingDirName = Path.GetDirectoryName(ProjectDirName)!;
        return ContainingDirName == SolutionParentDirectory ? ContainingDirName : Path.GetDirectoryName(ContainingDirName)!;
    }
}