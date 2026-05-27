using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using AutomationTool;
using UnrealSharp.Automation.Processes;
using UnrealSharp.Automation.Utilities;

namespace UnrealSharp.Automation.BuildCommands;

[Help("Generates a new C# project in the specified folder.")]
[Help("ProjectFolder=<Path>", "The root directory of the Unreal Engine project.")]
[Help("ProjectRoot=<Path>", "The root directory for the generated C# project.")]
[Help("ProjectName=<Name>", "The name of the new C# project to generate.")]
[Help("CreateModuleClass", "Whether to create a default module class in the generated project.")]
[Help("SkipSolutionGeneration", "If set, the .sln file will not be regenerated.")]
[Help("SkipUSharpProjSetup", "If set, the generated .csproj will not be modified to include UnrealSharp properties and dependencies.")]
[Help("EditorOnly", "If set, the generated project will be marked as not publishable.")]
[Help("Dependencies=<Path>+<Path>", "Additional project dependencies to include in the generated .csproj file.")]
public class GenerateProject : BuildCommand
{
    private const string CsProjFileExtension = "csproj";
    private const string CsFileExtension = "cs";
    private const string CsprojTemplateName = "Csproj";
    private const string ModuleTemplateName = "Module";
    private const string SkipIncludeAnalyzersPropertyName = "SkipIncludeAnalyzers";
    private const string IsPublishablePropertyName = "IsPublishable";
    private const string IsEditorOnlyPropertyName = "IsEditorOnly";
    private const string DisableWithEditorCondition = "'$(DisableWithEditor)' == 'true'";

    public override void ExecuteBuild()
    {
        string ProjectFolder = ParseRequiredStringParam("ProjectFolder");
        string ProjectRoot = ParseRequiredStringParam("ProjectRoot");
        string ProjectName = ParseRequiredStringParam("ProjectName");
        bool CreateModuleClass = ParseParam("CreateModuleClass");
        bool SkipSolutionGeneration = ParseParam("SkipSolutionGeneration");
        bool SkipUSharpProjSetup = ParseParam("SkipUSharpProjSetup");
        bool EditorOnly = ParseParam("EditorOnly");
        string[] Dependencies = ParseParamValues("Dependencies");

        if (!ProjectUtilities.ContainsUPluginOrUProjectFile(ProjectRoot))
        {
            throw new AutomationException($"ProjectRoot '{ProjectRoot}' must contain a .uplugin or .uproject file at its top level.");
        }

        if (ProjectFolder == ProjectRoot)
        {
            ProjectFolder = Path.Combine(ProjectFolder, this.GetScriptDirectoryName());
        }

        string CsProjFileName = $"{ProjectName}.{CsProjFileExtension}";
        string ProjectPath = Path.Combine(ProjectFolder, CsProjFileName);

        WriteProjectTemplate(ProjectName, ProjectFolder);

        if (CreateModuleClass)
        {
            WriteModuleTemplate(ProjectName, ProjectFolder);
        }

        UpdateCsprojDocument(ProjectPath, ProjectFolder, Dependencies, EditorOnly);

        LoggerUtilities.LogUnrealSharpInfo($"Generated project '{ProjectName}' successfully.");

        if (!SkipSolutionGeneration)
        {
            GenerateSolution.GenerateManagedSolution(this);
        }

        if (!SkipUSharpProjSetup)
        {
            AddLaunchSettings(ProjectFolder);
            BuildProject(ProjectPath, ProjectFolder);
        }
    }

    private void WriteProjectTemplate(string projectName, string projectFolder)
    {
        Dictionary<string, string> TemplateValues = new Dictionary<string, string>
        {
            { "DOTNET_VERSION", DotNetUtilities.GetVersion() }
        };

        TemplateUtilities.WriteTemplateToFile(this, CsprojTemplateName, projectName, CsProjFileExtension, projectFolder, TemplateValues);
    }

    private void WriteModuleTemplate(string projectName, string projectFolder)
    {
        Dictionary<string, string> ModuleTemplateValues = new Dictionary<string, string>
        {
            { "MODULE_NAME", projectName }
        };

        TemplateUtilities.WriteTemplateToFile(this, ModuleTemplateName, projectName, CsFileExtension, projectFolder, ModuleTemplateValues);
    }

    private void UpdateCsprojDocument(string projectPath, string projectFolder, IReadOnlyList<string>? dependencies, bool isEditorOnly)
    {
        try
        {
            XmlDocument CsprojDocument = new XmlDocument();
            CsprojDocument.Load(projectPath);
            CsprojDocument.EnsureProjectRoot();

            ApplyEditorOnlyFlags(CsprojDocument, isEditorOnly);
            ApplyAnalyzerFlag(CsprojDocument);
            ApplySharedPropsImport(CsprojDocument, projectFolder);
            ApplyDependencies(CsprojDocument, projectFolder, dependencies);

            CsprojDocument.Save(projectPath);
        }
        catch (Exception Exception)
        {
            throw new AutomationException($"Failed to update the generated project '{projectPath}'. See inner exception for details.", Exception);
        }
    }

    private static void ApplyEditorOnlyFlags(XmlDocument csprojDocument, bool isEditorOnly)
    {
        if (!isEditorOnly)
        {
            return;
        }

        csprojDocument.SetProjectProperty(IsPublishablePropertyName, "false", DisableWithEditorCondition);
        csprojDocument.SetProjectProperty(IsEditorOnlyPropertyName, "true");
    }

    private void ApplyAnalyzerFlag(XmlDocument csprojDocument)
    {
        csprojDocument.SetProjectProperty(SkipIncludeAnalyzersPropertyName, ParseParam(SkipIncludeAnalyzersPropertyName) ? "true" : "false");
    }

    private void ApplySharedPropsImport(XmlDocument csprojDocument, string projectFolder)
    {
        string UnrealSharpPluginPath = this.GetUnrealSharpSharedPropsPath();
        string RelativeUnrealSharpPath = CsProjectUtilities.GetRelativePath(projectFolder, UnrealSharpPluginPath);
        csprojDocument.MakeProjectImport(csprojDocument.DocumentElement!, RelativeUnrealSharpPath);
    }

    private static void ApplyDependencies(XmlDocument csprojDocument, string projectFolder, IReadOnlyList<string>? dependencies)
    {
        if (dependencies is null || dependencies.Count == 0)
        {
            return;
        }

        XmlElement ItemGroup = CsProjectUtilities.GetOrCreateItemGroup(csprojDocument);
        CsProjectUtilities.AddProjectReferences(csprojDocument, ItemGroup, projectFolder, dependencies);
    }

    private void AddLaunchSettings(string projectFolder)
    {
        LaunchSettingsScaffolding.EnsureProjectLaunchSettings(this, projectFolder);
    }

    private static void BuildProject(string projectPath, string projectFolder)
    {
        using DotnetProcess BuildProjectProcess = new DotnetProcess();
        BuildProjectProcess.StartInfo.ArgumentList.Add("build");
        BuildProjectProcess.StartInfo.ArgumentList.Add(projectPath);
        BuildProjectProcess.StartInfo.WorkingDirectory = projectFolder;
        BuildProjectProcess.StartProcess();
    }
}