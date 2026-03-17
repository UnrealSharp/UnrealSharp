using Microsoft.VisualStudio.SolutionPersistence;
using Microsoft.VisualStudio.SolutionPersistence.Model;
using Microsoft.VisualStudio.SolutionPersistence.Serializer;

namespace UnrealSharpBuildTool.Actions;

public class MergeSolution : BuildToolAction
{
    public override bool RunAction()
    {
        string nativeSlnPath = Path.Combine(Program.GetProjectDirectory(), Program.BuildToolOptions.ProjectName + ".sln");
        string managedSlnxPath = Path.Combine(Program.GetScriptFolder(), $"{Program.GetProjectNameAsManaged()}.slnx");
        string mixedSlnPath = Path.ChangeExtension(nativeSlnPath, null) + ".Mixed.sln";

        if (!File.Exists(nativeSlnPath))
        {
            Console.WriteLine($"Failed to load native solution: {nativeSlnPath}");
            return false;
        }

        if (!File.Exists(managedSlnxPath))
        {
            Console.WriteLine($"Failed to load managed solution: {managedSlnxPath}");
            return false;
        }

        return MergeSolutionsAsync(nativeSlnPath, managedSlnxPath, mixedSlnPath).GetAwaiter().GetResult();
    }

    private static async Task<bool> MergeSolutionsAsync(string nativeSlnPath, string managedSlnxPath, string mixedSlnPath)
    {
        ISolutionSerializer? nativeSerializer = SolutionSerializers.GetSerializerByMoniker(nativeSlnPath);
        ISolutionSerializer? managedSerializer = SolutionSerializers.GetSerializerByMoniker(managedSlnxPath);

        if (nativeSerializer is null || managedSerializer is null)
        {
            Console.WriteLine("Failed to resolve solution serializers.");
            return false;
        }

        SolutionModel nativeSolution = await nativeSerializer.OpenAsync(nativeSlnPath, CancellationToken.None);
        SolutionModel managedSolution = await managedSerializer.OpenAsync(managedSlnxPath, CancellationToken.None);

        string sourceDirectory = Path.GetDirectoryName(managedSlnxPath)!;
        string targetDirectory = Path.GetDirectoryName(nativeSlnPath)!;

        MergeProjects(nativeSolution, managedSolution, sourceDirectory, targetDirectory);

        await SolutionSerializers.SlnFileV12.SaveAsync(mixedSlnPath, nativeSolution, CancellationToken.None);

        Console.WriteLine($"Successfully created mixed solution: {mixedSlnPath}");
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

            string rebasedPath = RebasePath(project.FilePath, sourceDirectory, targetDirectory);
            SolutionProjectModel added = target.AddProject(rebasedPath, folder: null);
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