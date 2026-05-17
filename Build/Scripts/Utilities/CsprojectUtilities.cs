using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;

namespace UnrealSharp.Automation.Utilities;

public static class CsProjectUtilities
{
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

        XmlNodeList? ExistingReferences = document.SelectNodes("//ItemGroup/ProjectReference");
        HashSet<string> ExistingIncludes = ExistingReferences!
            .OfType<XmlElement>()
            .Select(element => element.GetAttribute("Include"))
            .Where(include => !string.IsNullOrEmpty(include))
            .ToHashSet(Comparer);

        bool Modified = false;
        
        foreach (string Dependency in dependencies)
        {
            if (string.IsNullOrWhiteSpace(Dependency))
            {
                continue;
            }

            string RelativePath = GetRelativePath(projectFolder, Dependency);
            if (!ExistingIncludes.Add(RelativePath))
            {
                continue;
            }

            XmlElement ProjectReference = document.CreateElement("ProjectReference");
            ProjectReference.SetAttribute("Include", RelativePath);
            itemGroup.AppendChild(ProjectReference);
            Modified = true;
        }

        return Modified;
    }
}