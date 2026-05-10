using System.Xml;
using CommandLine;

namespace UnrealSharpBuildTool.Actions;

[Verb("UpdateProjectDependenciesParameters", aliases: ["UpdateProjectDependencies"], HelpText = "Updates the specified .csproj file to include dependencies as ProjectReferences.")]
public struct UpdateProjectDependenciesParameters
{
    [Option("ProjectPath", Required = true, HelpText = "The path to the .csproj file to update.")]
    public string ProjectPath { get; set; }

    [Option("Dependencies", Required = false, HelpText = "The list of dependencies to add as ProjectReferences.")]
    public IEnumerable<string>? Dependencies { get; set; }
}

public static class UpdateProjectDependenciesAction
{
    public static void UpdateProjectDependencies(UpdateProjectDependenciesParameters parameters)
    {
        if (!File.Exists(parameters.ProjectPath))
        {
            throw new FileNotFoundException("The specified project file does not exist.", parameters.ProjectPath);
        }

        string projectFolder = Directory.GetParent(parameters.ProjectPath)!.FullName;

        Console.WriteLine($"Project Path: {parameters.ProjectPath}");
        Console.WriteLine($"Project Folder: {projectFolder}");

        UpdateProject(parameters.ProjectPath, projectFolder, parameters.Dependencies ?? Enumerable.Empty<string>());
    }

    private static void UpdateProject(string projectPath, string projectFolder, IEnumerable<string> dependencies)
    {
        try
        {
            XmlDocument csprojDocument = new XmlDocument();
            csprojDocument.Load(projectPath);

            XmlNodeList itemGroups = csprojDocument.SelectNodes("//ItemGroup")!;

            HashSet<string> existingDependencies = itemGroups
                .OfType<XmlElement>()
                .SelectMany(x => x.ChildNodes.OfType<XmlElement>())
                .Where(x => x.Name == "ProjectReference")
                .Select(x => x.GetAttribute("Include"))
                .ToHashSet();

            // Find an existing ItemGroup or create a new one
            XmlElement? targetItemGroup = itemGroups.OfType<XmlElement>().FirstOrDefault();
            
            if (targetItemGroup is null)
            {
                targetItemGroup = csprojDocument.CreateElement("ItemGroup");
                csprojDocument.DocumentElement!.AppendChild(targetItemGroup);
            }

            bool wasModified = false;
            foreach (string dependency in dependencies)
            {
                string relativePath = GetRelativePath(projectFolder, dependency);

                if (existingDependencies.Contains(relativePath))
                {
                    continue;
                }

                XmlElement projectReference = csprojDocument.CreateElement("ProjectReference");
                projectReference.SetAttribute("Include", relativePath);
                targetItemGroup.AppendChild(projectReference);
                wasModified = true;
                
                Console.WriteLine($"Added dependency: {relativePath}");
            }

            if (wasModified)
            {
                csprojDocument.Save(projectPath);
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"An error occurred while updating the .csproj file: {ex.Message}", ex);
        }
    }

    public static string GetRelativePath(string basePath, string targetPath)
    {
        if (!basePath.EndsWith(Path.DirectorySeparatorChar.ToString()))
        {
            basePath += Path.DirectorySeparatorChar;
        }

        Uri baseUri = new Uri(basePath);
        Uri targetUri = new Uri(targetPath);
        Uri relativeUri = baseUri.MakeRelativeUri(targetUri);
        
        string relativePath = Uri.UnescapeDataString(relativeUri.ToString());
        
        return OperatingSystem.IsWindows() 
            ? relativePath.Replace('/', '\\') 
            : relativePath.Replace('\\', '/');
    }
}