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
    private const string PropertiesFolderName = "Properties";
    private const string LaunchSettingsFileName = "launchSettings.json";

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
        string OutputProjectFolder = Path.Combine(ProjectFolder, ProjectName);
        string ProjectPath = Path.Combine(OutputProjectFolder, CsProjFileName);

        Dictionary<string, string> TemplateValues = new Dictionary<string, string>
        {
            { "DOTNET_VERSION", DotNetUtilities.GetVersion() }
        };

        TemplateUtilities.WriteTemplateToFile(this, "Csproj", ProjectName, CsProjFileExtension, OutputProjectFolder, TemplateValues);

        if (CreateModuleClass)
        {
            Dictionary<string, string> ModuleTemplateValues = new Dictionary<string, string>
            {
                { "MODULE_NAME", ProjectName }
            };

            TemplateUtilities.WriteTemplateToFile(this, "Module", ProjectName, "cs", OutputProjectFolder, ModuleTemplateValues);
        }

        UpdateCsprojDocument(ProjectPath, OutputProjectFolder, Dependencies, EditorOnly);
        
        LoggerUtilities.LogUnrealSharpInfo($"Generated project '{ProjectName}' successfully.");
        
        if (!SkipSolutionGeneration)
        {
            GenerateSolution.GenerateManagedSolution(this);
        }

        if (!SkipUSharpProjSetup)
        {
            AddLaunchSettings(OutputProjectFolder);
            BuildProject(ProjectPath, OutputProjectFolder);
        }
    }

    private void UpdateCsprojDocument(string projectPath, string projectFolder, IEnumerable<string>? dependencies, bool isEditorOnly)
    {
        try
        {
            XmlDocument CsprojDocument = new XmlDocument();
            CsprojDocument.Load(projectPath);
            CsprojDocument.EnsureProjectRoot();

            XmlElement ItemGroup = CsProjectUtilities.GetOrCreateItemGroup(CsprojDocument);

            if (isEditorOnly)
            {
                CsprojDocument.SetProjectProperty("IsPublishable", "false", "'$(DisableWithEditor)' == 'true'");
                CsprojDocument.SetProjectProperty("IsEditorOnly", "true");
            }

            string UnrealSharpPluginPath = this.GetUnrealSharpSharedPropsPath();
            string RelativeUnrealSharpPath = CsProjectUtilities.GetRelativePath(projectFolder, UnrealSharpPluginPath);
            CsprojDocument.MakeProjectImport(CsprojDocument.DocumentElement!, RelativeUnrealSharpPath);

            if (dependencies != null)
            {
                CsProjectUtilities.AddProjectReferences(CsprojDocument, ItemGroup, projectFolder, dependencies);
            }

            CsprojDocument.Save(projectPath);
        }
        catch (Exception Exception)
        {
            throw new AutomationException($"Failed to update the generated project '{projectPath}'. See inner exception for details.", Exception);
        }
    }

    private void AddLaunchSettings(string projectFolder)
    {
        string CsProjectPath = Path.Combine(this.GetProjectScriptFolder(), projectFolder);
        string PropertiesDirectoryPath = Path.Combine(CsProjectPath, PropertiesFolderName);
        string LaunchSettingsPath = Path.Combine(PropertiesDirectoryPath, LaunchSettingsFileName);

        if (!Directory.Exists(PropertiesDirectoryPath))
        {
            Directory.CreateDirectory(PropertiesDirectoryPath);
        }

        if (File.Exists(LaunchSettingsPath))
        {
            return;
        }

        LaunchSettingsUtilities.CreateOrUpdateLaunchSettings(this, LaunchSettingsPath);
    }

    private static void BuildProject(string projectPath, string projectFolder)
    {
        DotnetProcess BuildProjectProcess = new DotnetProcess();
        BuildProjectProcess.StartInfo.ArgumentList.Add("build");
        BuildProjectProcess.StartInfo.ArgumentList.Add(projectPath);
        BuildProjectProcess.StartInfo.WorkingDirectory = projectFolder;
        BuildProjectProcess.StartProcess();
    }
}