using System.Collections.Immutable;
using System.Xml;

namespace UnrealSharpBuildTool.Actions;

public class UpdateProjectDependencies : BuildToolAction
{
    private string _projectPath = string.Empty;
    private string _projectFolder = string.Empty;
    private ImmutableList<string> _dependencies = ImmutableList<string>.Empty;

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
            var csprojDocument = new XmlDocument();
            csprojDocument.Load(_projectPath);

            var itemGroups = csprojDocument.SelectNodes("//ItemGroup")!;

            var existingDependencies = itemGroups
                    .OfType<XmlElement>()
                    .Where(x => x.Name == "ProjectReference")
                    .Select(x => x.GetAttribute("Include"))
                    .ToHashSet();

            var newItemGroup = itemGroups.OfType<XmlElement>().FirstOrDefault();
            if (newItemGroup is null)
            {
                newItemGroup = csprojDocument.CreateElement("ItemGroup");
                csprojDocument.DocumentElement!.AppendChild(newItemGroup);
            }

            foreach (var dependency in _dependencies.Except(existingDependencies))
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
        string relativePath = GenerateProject.GetRelativePath(_projectFolder, dependency);

        XmlElement generatedCode = doc.CreateElement("ProjectReference");
        generatedCode.SetAttribute("Include", relativePath);
        itemGroup.AppendChild(generatedCode);
    }
}
