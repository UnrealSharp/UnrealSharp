using System;
using System.IO;
using System.Xml;
using AutomationTool;
using UnrealSharp.Automation.Utilities;

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

        if (Dependencies.Length == 0)
        {
            // Nothing to do, document is unchanged.
            return;
        }

        string ProjectFolder = Directory.GetParent(ProjectPath)?.FullName ?? throw new AutomationException($"Could not determine parent directory of '{ProjectPath}'.");
        UpdateProject(ProjectPath, ProjectFolder, Dependencies);
    }

    private static void UpdateProject(string projectPath, string projectFolder, string[] dependencies)
    {
        try
        {
            XmlDocument CsprojDocument = new XmlDocument();
            CsprojDocument.Load(projectPath);

            XmlElement TargetItemGroup = CsProjectUtilities.GetOrCreateItemGroup(CsprojDocument);

            if (CsProjectUtilities.AddProjectReferences(CsprojDocument, TargetItemGroup, projectFolder, dependencies))
            {
                CsprojDocument.Save(projectPath);
            }
        }
        catch (Exception Exception)
        {
            throw new AutomationException($"Failed to update project dependencies for '{projectPath}'. See inner exception for details.", Exception);
        }
    }
}