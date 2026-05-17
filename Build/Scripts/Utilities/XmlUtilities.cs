using System;
using System.IO;
using System.Linq;
using System.Xml;

namespace UnrealSharp.Automation.Utilities;

public static class XmlUtilities
{
    public static XmlElement EnsureProjectRoot(this XmlDocument doc)
    {
        if (doc.DocumentElement != null)
        {
            return doc.DocumentElement;
        }

        XmlElement Project = doc.CreateElement("Project");
        doc.AppendChild(Project);
        return Project;
    }

    public static XmlElement MakeItemGroup(this XmlDocument doc, XmlElement root)
    {
        XmlElement ItemGroup = doc.CreateElement("ItemGroup");
        root.AppendChild(ItemGroup);
        return ItemGroup;
    }

    public static XmlElement FindOrMakeItemGroup(this XmlDocument doc, XmlElement root)
    {
        XmlNode? Existing = root.SelectSingleNode("ItemGroup");

        if (Existing != null && Existing is XmlElement Element)
        {
            return Element;
        }

        return doc.MakeItemGroup(root);
    }

    public static XmlElement MakePropertyGroup(this XmlDocument doc, XmlElement root)
    {
        XmlElement PropertyGroup = doc.CreateElement("PropertyGroup");
        root.AppendChild(PropertyGroup);
        return PropertyGroup;
    }

    public static XmlElement MakeProjectImport(this XmlDocument doc, XmlElement root, string projectPath)
    {
        XmlElement Import = doc.CreateElement("Import");
        Import.SetAttribute("Project", projectPath);
        root.AppendChild(Import);
        return Import;
    }

    public static XmlElement GetOrCreateChild(this XmlDocument doc, XmlElement parent, string name)
    {
        XmlNode? Existing = parent.SelectSingleNode(name);
        if (Existing != null && Existing is XmlElement)
        {
            return (XmlElement)Existing;
        }

        XmlElement Created = doc.CreateElement(name);
        parent.AppendChild(Created);
        return Created;
    }

    public static void AppendPackageReference(this XmlDocument doc, XmlElement itemGroup, string packageName, string packageVersion)
    {
        XmlElement PackageReference = doc.CreateElement("PackageReference");
        PackageReference.SetAttribute("Include", packageName);
        PackageReference.SetAttribute("Version", packageVersion);
        itemGroup.AppendChild(PackageReference);
    }

    public static void AppendReference(this XmlDocument doc, XmlElement itemGroup, string referenceName, string binPath)
    {
        XmlElement ReferenceElement = doc.CreateElement("Reference");
        ReferenceElement.SetAttribute("Include", referenceName);

        XmlElement HintPath = doc.CreateElement("HintPath");
        HintPath.InnerText = Path.Combine(binPath, DotNetUtilities.GetVersion(), referenceName + ".dll");
        ReferenceElement.AppendChild(HintPath);

        itemGroup.AppendChild(ReferenceElement);
    }

    public static XmlElement AppendAnalyzer(this XmlDocument doc, XmlElement itemGroup, string includePath)
    {
        XmlElement Analyzer = doc.CreateElement("Analyzer");
        Analyzer.SetAttribute("Include", includePath);
        itemGroup.AppendChild(Analyzer);
        return Analyzer;
    }

    public static XmlElement FindOrMakeLabelGroup(
        this XmlDocument doc,
        XmlElement projectRoot,
        string groupName,
        string label,
        bool clearExisting)
    {
        string NamespaceUri = projectRoot.NamespaceURI;

        XmlElement? FirstFound = null;
        for (XmlNode XmlNode = projectRoot.FirstChild!; XmlNode != null; XmlNode = XmlNode.NextSibling!)
        {
            if (XmlNode is not XmlElement Element)
            {
                continue;
            }

            if (Element.LocalName != groupName)
            {
                continue;
            }

            string FoundLabel = Element.GetAttribute("Label");
            if (!string.Equals(FoundLabel, label, StringComparison.Ordinal))
            {
                continue;
            }

            FirstFound = Element;
            break;
        }

        if (FirstFound == null)
        {
            FirstFound = doc.CreateElement(groupName, NamespaceUri);
            projectRoot.AppendChild(FirstFound);
        }
        else if (clearExisting)
        {
            FirstFound.RemoveAll();
        }

        return FirstFound;
    }

    public static XmlElement FindOrMakeGeneratedGroup(this XmlDocument doc, XmlElement root, string groupName, string label)
    {
        string NewLabel = label + "_Generated";
        XmlElement GeneratedGroup = FindOrMakeLabelGroup(doc, root, groupName, NewLabel, true);

        GeneratedGroup.AppendChild(doc.CreateComment($"Don't edit this group, it is managed by UnrealSharpBuildTool and will be overwritten. Free to add other items outside of this group."));
        GeneratedGroup.SetAttribute("Label", NewLabel);
        return GeneratedGroup;
    }

    public static XmlElement FindOrMakeGeneratedLabeledItemGroup(this XmlDocument doc, XmlElement root, string label) => FindOrMakeGeneratedGroup(doc, root, "ItemGroup", label);
    public static XmlElement FindOrMakeGeneratedLabeledPropertyGroup(this XmlDocument doc, XmlElement root, string label) => FindOrMakeGeneratedGroup(doc, root, "PropertyGroup", label);

    public static void SetProjectProperty(this XmlDocument doc, string propertyName, string value, string? condition = null)
    {
        XmlElement Root = doc.DocumentElement!;

        XmlElement? PropertyGroup = Root
            .SelectNodes("PropertyGroup")?
            .OfType<XmlElement>()
            .FirstOrDefault(pg => !pg.HasAttribute("Condition"));

        if (PropertyGroup == null)
        {
            PropertyGroup = doc.CreateElement("PropertyGroup");
            Root.AppendChild(PropertyGroup);
        }

        XmlElement Property = PropertyGroup[propertyName] ?? doc.CreateElement(propertyName);
        Property.InnerText = value;

        if (!string.IsNullOrEmpty(condition))
        {
            Property.SetAttribute("Condition", condition);
        }

        if (Property.ParentNode == null)
        {
            PropertyGroup.AppendChild(Property);
        }
    }
}
