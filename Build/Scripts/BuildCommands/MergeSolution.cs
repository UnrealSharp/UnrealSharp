using AutomationTool;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnrealSharp.Automation.Utilities;
using Microsoft.VisualStudio.SolutionPersistence;
using Microsoft.VisualStudio.SolutionPersistence.Model;
using Microsoft.VisualStudio.SolutionPersistence.Serializer;
using System.Linq;

namespace UnrealSharp.Automation.BuildCommands;

[Help("Merges native and user solution files using MSBuild.")]
public class MergeSolution : BuildCommand
{
    public override void ExecuteBuild()
    {
        string nativeSlnxPath = Path.Combine(this.GetProjectRootFolder(), this.GetProjectName() + ".slnx");
        string managedSlnxPath = Path.Combine(this.GetProjectScriptFolder(), $"{this.GetProjectNameAsManaged()}.slnx");
        string mixedSlnxPath = Path.ChangeExtension(nativeSlnxPath, null) + ".Mixed.slnx";

        if (!File.Exists(nativeSlnxPath))
        {
            throw new AutomationException($"Failed to load native solution: {nativeSlnxPath}.");
        }

        if (!File.Exists(managedSlnxPath))
        {
            throw new AutomationException($"Failed to load managed solution: {managedSlnxPath}.");
        }

        MergeSolutionsAsync(nativeSlnxPath, managedSlnxPath, mixedSlnxPath).GetAwaiter().GetResult();
    }

    private static async Task<bool> MergeSolutionsAsync(string nativeSlnxPath, string managedSlnxPath, string mixedSlnxPath)
    {
        ISolutionSerializer? nativeSerializer = SolutionSerializers.GetSerializerByMoniker(nativeSlnxPath);
        ISolutionSerializer? managedSerializer = SolutionSerializers.GetSerializerByMoniker(managedSlnxPath);

        if (nativeSerializer is null || managedSerializer is null)
        {
            throw new AutomationException("Failed to get solution serializer for one or both solution files.");
        }

        SolutionModel nativeSolution = await nativeSerializer.OpenAsync(nativeSlnxPath, CancellationToken.None);
        SolutionModel managedSolution = await managedSerializer.OpenAsync(managedSlnxPath, CancellationToken.None);

        string sourceDirectory = Path.GetDirectoryName(managedSlnxPath)!;
        string targetDirectory = Path.GetDirectoryName(nativeSlnxPath)!;

        MergeProjects(nativeSolution, managedSolution, sourceDirectory, targetDirectory);

        await SolutionSerializers.SlnXml.SaveAsync(mixedSlnxPath, nativeSolution, CancellationToken.None);

        LoggerUtilities.LogUnrealSharpInfo($"Successfully created mixed solution: {mixedSlnxPath}");
        return true;
    }

    private static void MergeProjects(
        SolutionModel target,
        SolutionModel source,
        string sourceDirectory,
        string targetDirectory)
    {
        foreach (SolutionProjectModel project in source.SolutionProjects)
        {
            if (project.Parent is SolutionFolderModel)
            {
                continue;
            }
            SolutionFolderModel? gamesFolderModel = target.FindFolder("/Games/");
            if (gamesFolderModel == null)
            {
                throw new AutomationException("Failed to find the 'Games' folder in the target solution.");
            }

            string rebasedPath = RebasePath(project.FilePath, sourceDirectory, targetDirectory);
            SolutionProjectModel added = target.AddProject(rebasedPath, folder: gamesFolderModel);
            ApplyManagedConfigurationRules(added);
        }

        // Recursively merge folders and their contained projects.
        foreach (SolutionFolderModel folder in source.SolutionFolders.Where(f => f.Parent is null))
        {
            MergeFolder(target, source, folder, sourceDirectory, targetDirectory);
        }
    }

    private static void MergeFolder(
        SolutionModel target,
        SolutionModel source,
        SolutionFolderModel sourceFolder,
        string sourceDirectory,
        string targetDirectory)
    {
        string folderPath = $"/{sourceFolder.Name.Trim('/')}/";
        SolutionFolderModel targetFolder = target.AddFolder(folderPath);

        foreach (SolutionProjectModel project in source.SolutionProjects.Where(p => p.Parent == sourceFolder))
        {
            string rebasedPath = RebasePath(project.FilePath, sourceDirectory, targetDirectory);
            SolutionProjectModel added = target.AddProject(rebasedPath, folder: targetFolder);
            ApplyManagedConfigurationRules(added);
        }

        // Recurse into sub-folders.
        foreach (SolutionFolderModel subFolder in source.SolutionFolders.Where(f => f.Parent == sourceFolder))
        {
            MergeFolder(target, source, subFolder, sourceDirectory, targetDirectory);
        }
    }

    /// <summary>
    /// Applies configuration rules to a managed C# project so that every native solution
    /// configuration (e.g. "Development Editor|Win64") maps to Debug|AnyCPU.
    /// Using empty string for SolutionBuildType/SolutionPlatform acts as a wildcard,
    /// matching all solution-level configurations.
    /// </summary>
    private static void ApplyManagedConfigurationRules(SolutionProjectModel project)
    {
        // Map all solution build types - Debug
        project.AddProjectConfigurationRule(new ConfigurationRule(
            BuildDimension.BuildType,
            solutionBuildType: string.Empty,
            solutionPlatform: string.Empty,
            projectValue: "Debug"));

        // Map all solution platforms - AnyCPU
        project.AddProjectConfigurationRule(new ConfigurationRule(
            BuildDimension.Platform,
            solutionBuildType: string.Empty,
            solutionPlatform: string.Empty,
            projectValue: "AnyCPU"));

        // Enable build for all configurations.
        project.AddProjectConfigurationRule(new ConfigurationRule(
            BuildDimension.Build,
            solutionBuildType: string.Empty,
            solutionPlatform: string.Empty,
            projectValue: "True"));
    }

    private static string RebasePath(string projectPath, string sourceDirectory, string targetDirectory)
    {
        string absolutePath = Path.GetFullPath(Path.Combine(sourceDirectory, projectPath));
        return Path.GetRelativePath(targetDirectory, absolutePath);
    }
}
