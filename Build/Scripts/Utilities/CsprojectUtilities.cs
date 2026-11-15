using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using AutomationTool;

namespace UnrealSharp.Automation.Utilities;

public static class CsProjectUtilities
{
    public static void UpdateUnrealSharpImports(BuildCommand command, IEnumerable<string> projectPaths)
    {
        string? SharedPropsPath = null;
        StringComparison Comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

        foreach (string ProjectPath in projectPaths)
        {
            XmlDocument Document = new XmlDocument { PreserveWhitespace = true };
            Document.Load(ProjectPath);

            // Only update the single literal import written by GenerateProject.
            XmlElement[] Imports = Document.DocumentElement!.ChildNodes.OfType<XmlElement>()
                .Where(element => element.LocalName == "Import")
                .Where(element => Path.GetFileName(element.GetAttribute("Project").Replace('\\', '/')) == "UnrealSharp.Shared.props")
                .ToArray();
            if (Imports.Length != 1 || Imports[0].HasAttribute("Condition"))
            {
                continue;
            }

            string ImportPath = Imports[0].GetAttribute("Project");
            if (ImportPath.Contains("$(") || ImportPath.Contains("@(") || ImportPath.Contains("%("))
            {
                continue;
            }

            SharedPropsPath ??= command.GetUnrealSharpSharedPropsPath();
            string ProjectDirectory = Path.GetDirectoryName(Path.GetFullPath(ProjectPath))!;
            string ExistingPath = Path.GetFullPath(ImportPath.Replace('\\', Path.DirectorySeparatorChar), ProjectDirectory);
            if (string.Equals(ExistingPath, SharedPropsPath, Comparison))
            {
                continue;
            }

            Imports[0].SetAttribute("Project", GetRelativePath(ProjectDirectory, SharedPropsPath));
            Document.Save(ProjectPath);
        }
    }

    public static string GetRelativePath(string basePath, string targetPath)
    {
        ArgumentException.ThrowIfNullOrEmpty(basePath);
        ArgumentException.ThrowIfNullOrEmpty(targetPath);

        string Relative = Path.GetRelativePath(basePath, targetPath);
        return OperatingSystem.IsWindows() ? Relative.Replace('/', '\\') : Relative.Replace('\\', '/');
    }
    
    public static XmlElement GetOrCreateItemGroup(XmlDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        if (document.DocumentElement is null)
        {
            throw new InvalidOperationException("The .csproj document has no root <Project> element.");
        }

        if (document.SelectSingleNode("//ItemGroup") is XmlElement ExistingItemGroup)
        {
            return ExistingItemGroup;
        }

        XmlElement NewItemGroup = document.CreateElement("ItemGroup");
        document.DocumentElement.AppendChild(NewItemGroup);
        return NewItemGroup;
    }
    
    public static bool AddProjectReferences(XmlDocument document, XmlElement itemGroup, string projectFolder, IEnumerable<string> dependencies)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(itemGroup);
        ArgumentException.ThrowIfNullOrEmpty(projectFolder);
        ArgumentNullException.ThrowIfNull(dependencies);

        StringComparer Comparer = OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;

        HashSet<string> ExistingProjects = document
            .SelectNodes("//ItemGroup/ProjectReference")!
            .OfType<XmlElement>()
            .Select(element => element.GetAttribute("Include"))
            .ToHashSet(Comparer);

        HashSet<string> ExistingDlls = document
            .SelectNodes("//ItemGroup/Reference")!
            .OfType<XmlElement>()
            .Select(element => element.GetAttribute("Include"))
            .ToHashSet(Comparer);

        bool Modified = false;
        
        foreach (string Dependency in dependencies)
        {
            if (string.IsNullOrWhiteSpace(Dependency))
            {
                continue;
            }

            bool IsDll = Path.GetExtension(Dependency).Equals(".dll", StringComparison.OrdinalIgnoreCase);

            string RelativePath = GetRelativePath(projectFolder, Dependency);
            string DependencyName = Path.GetFileNameWithoutExtension(Dependency);

            bool WasAdded = IsDll
                ? ExistingDlls.Add(DependencyName)
                : ExistingProjects.Add(RelativePath);

            if (!WasAdded)
            {
                continue;
            }

            string ReferenceType = IsDll ? "Reference" : "ProjectReference";
            XmlElement ProjectReference = document.CreateElement(ReferenceType);
            if (IsDll)
            {
                ProjectReference.SetAttribute("Include", DependencyName);

                XmlElement HintPath = document.CreateElement("HintPath");
                HintPath.InnerText = RelativePath;
                ProjectReference.AppendChild(HintPath);
            }
            else
            {
                ProjectReference.SetAttribute("Include", RelativePath);
            }

            itemGroup.AppendChild(ProjectReference);
            Modified = true;
        }

        return Modified;
    }
}
