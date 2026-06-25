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
        string Relative = Path.GetRelativePath(basePath, targetPath);
        return OperatingSystem.IsWindows() ? Relative.Replace('/', '\\') : Relative.Replace('\\', '/');
    }
    
    public static XmlElement GetOrCreateItemGroup(XmlDocument document)
    {
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
        XmlNodeList? ExistingReferences = document.SelectNodes("//ItemGroup/ProjectReference");
        
        if (ExistingReferences == null)
        {
            throw new InvalidOperationException("Failed to query existing <ProjectReference> elements.");
        }
        
        HashSet<string> ExistingIncludes = ExistingReferences
            .OfType<XmlElement>()
            .Select(element => element.GetAttribute("Include"))
            .Where(include => !string.IsNullOrEmpty(include))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

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
    
    public static bool AddReferences(XmlDocument document, XmlElement itemGroup, string projectFolder, IEnumerable<string> assemblyPaths)
    {
        XmlNodeList? ExistingReferences = document.SelectNodes("//ItemGroup/Reference");

        if (ExistingReferences == null)
        {
            throw new InvalidOperationException("Failed to query existing <Reference> elements.");
        }
        
        HashSet<string> ExistingIncludes = ExistingReferences
            .OfType<XmlElement>()
            .Select(element => element.GetAttribute("Include"))
            .Where(include => !string.IsNullOrEmpty(include))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        bool Modified = false;

        foreach (string AssemblyPath in assemblyPaths)
        {
            string AssemblyName = Path.GetFileNameWithoutExtension(AssemblyPath);
            string RelativeHintPath = GetRelativePath(projectFolder, AssemblyPath);
            
            if (!ExistingIncludes.Add(AssemblyName))
            {
                continue;
            }
            
            XmlElement ReferenceElement = document.CreateElement("Reference");
            ReferenceElement.SetAttribute("Include", AssemblyName);
            
            XmlElement HintPathElement = document.CreateElement("HintPath");
            HintPathElement.InnerText = RelativeHintPath;
        
            ReferenceElement.AppendChild(HintPathElement);
            itemGroup.AppendChild(ReferenceElement);
        
            Modified = true;
        }

        return Modified;
    }
}