using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using AutomationTool;

namespace UnrealSharp.Automation.BuildCommands;

[Help("Updates the specified .csproj file to include dependencies as ProjectReferences.")]
[Help("ProjectPath=<Path>", "The path to the .csproj file to update.")]
[Help("Dependencies=<Path>+<Path>", "The list of dependencies to add as ProjectReferences.")]
public class UpdateProjectDependencies : BuildCommand
{
    public override void ExecuteBuild()
    {
        string ProjectPath = ParseRequiredStringParam("ProjectPath");
        string[] Dependencies = ParseParamValues("Dependencies");

        if (!File.Exists(ProjectPath))
        {
            throw new FileNotFoundException("The specified project file does not exist.", ProjectPath);
        }

        string ProjectFolder = Directory.GetParent(ProjectPath)!.FullName;
        UpdateProject(ProjectPath, ProjectFolder, Dependencies);
    }

    private static void UpdateProject(string projectPath, string projectFolder, IEnumerable<string> dependencies)
    {
        try
        {
            XmlDocument CsprojDocument = new XmlDocument();
            CsprojDocument.Load(projectPath);

            XmlNodeList ItemGroups = CsprojDocument.SelectNodes("//ItemGroup")!;

            HashSet<string> ExistingDependencies = ItemGroups
                .OfType<XmlElement>()
                .SelectMany(x => x.ChildNodes.OfType<XmlElement>())
                .Where(x => x.Name == "ProjectReference")
                .Select(x => x.GetAttribute("Include"))
                .ToHashSet();

            // Find an existing ItemGroup or create a new one
            XmlElement? TargetItemGroup = ItemGroups.OfType<XmlElement>().FirstOrDefault();

            if (TargetItemGroup is null)
            {
                TargetItemGroup = CsprojDocument.CreateElement("ItemGroup");
                CsprojDocument.DocumentElement!.AppendChild(TargetItemGroup);
            }

            bool WasModified = false;
            foreach (string Dependency in dependencies)
            {
                string RelativePath = GetRelativePath(projectFolder, Dependency);

                if (ExistingDependencies.Contains(RelativePath))
                {
                    continue;
                }

                XmlElement ProjectReference = CsprojDocument.CreateElement("ProjectReference");
                ProjectReference.SetAttribute("Include", RelativePath);
                TargetItemGroup.AppendChild(ProjectReference);
                WasModified = true;
            }

            if (WasModified)
            {
                CsprojDocument.Save(projectPath);
            }
        }
        catch (Exception Ex)
        {
            throw new InvalidOperationException($"An error occurred while updating the .csproj file: {Ex.Message}", Ex);
        }
    }

    public static string GetRelativePath(string basePath, string targetPath)
    {
        if (!basePath.EndsWith(Path.DirectorySeparatorChar.ToString()))
        {
            basePath += Path.DirectorySeparatorChar;
        }

        Uri BaseUri = new Uri(basePath);
        Uri TargetUri = new Uri(targetPath);
        Uri RelativeUri = BaseUri.MakeRelativeUri(TargetUri);

        string RelativePath = Uri.UnescapeDataString(RelativeUri.ToString());

        return OperatingSystem.IsWindows()
            ? RelativePath.Replace('/', '\\')
            : RelativePath.Replace('\\', '/');
    }
}
