using System.Collections.Immutable;
using System.Xml;

namespace UnrealSharpBuildTool.Actions;

public class UpdateProjectDependencies : BuildToolAction
{
    private string _projectPath = string.Empty;
    private string _projectFolder = string.Empty;
    private ImmutableList<string> _dependencies = ImmutableList<string>.Empty;
    private HashSet<string> _existingDependencies = new HashSet<string>();

    public override bool RunAction()
    {
        _projectPath = Program.TryGetArgument("ProjectPath");
        _projectFolder = Directory.GetParent(_projectPath)!.FullName;
        _dependencies = Program.GetArguments("Dependency").ToImmutableList();

        Console.WriteLine($"Project Path: {_projectPath}");
        Console.WriteLine($"Project Folder: {_projectFolder}");

        UpdateProject();
        return true;
    }

    private void UpdateProject()
    {
        try
        {
            XmlDocument csprojDocument = new XmlDocument();
            csprojDocument.Load(_projectPath);

            XmlNodeList itemGroups = csprojDocument.SelectNodes("//ItemGroup")!;

            _existingDependencies = itemGroups
                    .OfType<XmlElement>()
                    .Where(x => x.Name == "ItemGroup")
                    .SelectMany(x => x.ChildNodes.OfType<XmlElement>())
                    .Where(x => x.Name == "ProjectReference")
                    .Select(x => x.GetAttribute("Include"))
                    .ToHashSet();

            XmlElement? newItemGroup = itemGroups.OfType<XmlElement>().FirstOrDefault();
            
            if (newItemGroup is null)
            {
                newItemGroup = csprojDocument.CreateElement("ItemGroup");
                csprojDocument.DocumentElement!.AppendChild(newItemGroup);
            }

            foreach (string dependency in _dependencies)
            {
                AddDependency(csprojDocument, newItemGroup, dependency);
            }

            csprojDocument.Save(_projectPath);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"An error occurred while updating the .csproj file: {ex.Message}", ex);
        }
    }

    private void AddDependency(XmlDocument doc, XmlElement itemGroup, string dependency)
    {
        string relativePath = BuildPropsEmitter.GetRelativePath(_projectFolder, dependency);
        
        if (_existingDependencies.Contains(relativePath))
        {
            return;
        }

        XmlElement generatedCode = doc.CreateElement("ProjectReference");
        generatedCode.SetAttribute("Include", relativePath);
        itemGroup.AppendChild(generatedCode);
    }
}
