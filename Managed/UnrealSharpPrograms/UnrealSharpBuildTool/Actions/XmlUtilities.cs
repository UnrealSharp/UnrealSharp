using System.Xml;

namespace UnrealSharpBuildTool.Actions;

public static class XmlUtilities
{
    public static XmlElement EnsureProjectRoot(this XmlDocument doc)
    {
        if (doc.DocumentElement != null)
        {
            return doc.DocumentElement;
        }

        XmlElement project = doc.CreateElement("Project");
        doc.AppendChild(project);
        return project;
    }

    public static XmlElement MakeItemGroup(this XmlDocument doc, XmlElement root)
    {
        XmlElement itemGroup = doc.CreateElement("ItemGroup");
        root.AppendChild(itemGroup);
        return itemGroup;
    }

    public static XmlElement FindOrMakeItemGroup(this XmlDocument doc, XmlElement root)
    {
        XmlNode? existing = root.SelectSingleNode("ItemGroup");
        
        if (existing != null && existing is XmlElement element)
        {
            return element;
        }

        return doc.MakeItemGroup(root);
    }

    public static XmlElement MakePropertyGroup(this XmlDocument doc, XmlElement root)
    {
        XmlElement propertyGroup = doc.CreateElement("PropertyGroup");
        root.AppendChild(propertyGroup);
        return propertyGroup;
    }
    
    public static XmlElement MakeProjectImport(this XmlDocument doc, XmlElement root, string projectPath)
    {
        XmlElement import = doc.CreateElement("Import");
        import.SetAttribute("Project", projectPath);
        root.AppendChild(import);
        return import;
    }

    public static XmlElement GetOrCreateChild(this XmlDocument doc, XmlElement parent, string name)
    {
        XmlNode? existing = parent.SelectSingleNode(name);
        if (existing != null && existing is XmlElement)
        {
            return (XmlElement)existing;
        }

        XmlElement created = doc.CreateElement(name);
        parent.AppendChild(created);
        return created;
    }

    public static void AppendPackageReference(this XmlDocument doc, XmlElement itemGroup, string packageName, string packageVersion)
    {
        XmlElement packageReference = doc.CreateElement("PackageReference");
        packageReference.SetAttribute("Include", packageName);
        packageReference.SetAttribute("Version", packageVersion);
        itemGroup.AppendChild(packageReference);
    }

    public static void AppendReference(this XmlDocument doc, XmlElement itemGroup, string referenceName, string binPath)
    {
        XmlElement referenceElement = doc.CreateElement("Reference");
        referenceElement.SetAttribute("Include", referenceName);

        XmlElement hintPath = doc.CreateElement("HintPath");
        hintPath.InnerText = Path.Combine(binPath, Program.GetVersion(), referenceName + ".dll");
        referenceElement.AppendChild(hintPath);

        itemGroup.AppendChild(referenceElement);
    }

    public static XmlElement AppendAnalyzer(this XmlDocument doc, XmlElement itemGroup, string includePath)
    {
        XmlElement analyzer = doc.CreateElement("Analyzer");
        analyzer.SetAttribute("Include", includePath);
        itemGroup.AppendChild(analyzer);
        return analyzer;
    }
    
    public static XmlElement FindOrMakeLabelGroup(
        this XmlDocument doc,
        XmlElement projectRoot,
        string groupName,
        string label, 
        bool clearExisting)
    {
        string namespaceUri = projectRoot.NamespaceURI;

        XmlElement? firstFound = null;
        for (XmlNode xmlxNode = projectRoot.FirstChild!; xmlxNode != null; xmlxNode = xmlxNode.NextSibling!)
        {
            if (xmlxNode is not XmlElement element)
            {
                continue;
            }

            if (element.LocalName != groupName)
            {
                continue;
            }

            string foundLabel = element.GetAttribute("Label");
            if (!string.Equals(foundLabel, label, StringComparison.Ordinal))
            {
                continue;
            }

            firstFound = element;
            break;
        }

        if (firstFound == null)
        {
            firstFound = doc.CreateElement(groupName, namespaceUri);
            projectRoot.AppendChild(firstFound);
        }
        else if (clearExisting)
        {
            firstFound.RemoveAll();
        }
        
        return firstFound;
    }

    public static XmlElement FindOrMakeGeneratedGroup(this XmlDocument doc, XmlElement root, string groupName, string label)
    {
        string newLabel = label + "_Generated";
        XmlElement generatedGroup = FindOrMakeLabelGroup(doc, root, groupName, newLabel, true);
        
        generatedGroup.AppendChild(doc.CreateComment($"Don't edit this group, it is managed by UnrealSharpBuildTool and will be overwritten. Free to add other items outside of this group."));
        generatedGroup.SetAttribute("Label", newLabel);
        return generatedGroup;
    }
    
    public static XmlElement FindOrMakeGeneratedLabeledItemGroup(this XmlDocument doc, XmlElement root, string label) => FindOrMakeGeneratedGroup(doc, root, "ItemGroup", label);
    public static XmlElement FindOrMakeGeneratedLabeledPropertyGroup(this XmlDocument doc, XmlElement root, string label) => FindOrMakeGeneratedGroup(doc, root, "PropertyGroup", label);
}
