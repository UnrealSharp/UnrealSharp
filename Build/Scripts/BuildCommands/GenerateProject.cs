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
[Help("ProjectName=<Name>", "The name of the new C# project to generate.")]
[Help("CreateModuleClass", "Whether to create a default module class in the generated project.")]
[Help("GenerateSolution", "If set, a solution file will be generated for the new project after generating the .csproj file.")]
[Help("RunUSharpProjectSetup", "If set, the UnrealSharp project setup will be run after generating the project, which includes adding launch settings and building the project.")]
[Help("EditorOnly", "If set, the generated project will be marked as not publishable.")]
[Help("Dependencies=<Path>+<Path>", "Additional project dependencies to include in the generated .csproj file.")]
[Help("AssemblyDependencies=<Path>+<Path>", "Additional assembly dependencies to include in the generated .csproj file. These should be paths to .dll files.")]
[Help("SkipIncludeAnalyzers", "If set, the generated .csproj will not reference the UnrealSharp analyzers.")]
public class GenerateProject : BuildCommand
{
    private const string CsProjFileExtension = "csproj";
    private const string CsFileExtension = "cs";
    private const string CsprojTemplateName = "Csproj";
    private const string ModuleTemplateName = "Module";
    private const string SkipIncludeAnalyzersPropertyName = "SkipIncludeAnalyzers";

    public override void ExecuteBuild()
    {
        string ProjectFolder = ParseRequiredStringParam("ProjectFolder");
        string ProjectName = ParseRequiredStringParam("ProjectName");
        bool CreateModuleClass = ParseParam("CreateModuleClass");
        bool ShouldGenerateSolution = ParseParam("GenerateSolution");
        bool RunUSharpProjectSetup = ParseParam("RunUSharpProjectSetup");
        bool EditorOnly = ParseParam("EditorOnly");
        string[] Dependencies = ParseParamValues("Dependencies");
        string[] References = ParseParamValues("References");
        string ProjectPath = Path.Combine(ProjectFolder, $"{ProjectName}.{CsProjFileExtension}");
        string[] CompileIncludeFolder = ParseParamValues("CompileIncludeFolder");

        WriteProjectTemplate(ProjectName, ProjectFolder);

        if (CreateModuleClass)
        {
            WriteModuleTemplate(ProjectName, ProjectFolder);
        }

        UpdateCsprojDocument(ProjectPath, ProjectFolder, Dependencies, References, EditorOnly, CompileIncludeFolder);

        LoggerUtilities.LogUnrealSharpInfo($"Generated project '{ProjectName}' successfully.");

        if (ShouldGenerateSolution)
        {
            List<KeyValuePair<string, string>> ActionArgs = new List<KeyValuePair<string, string>>
            {
                new("ForceGenerate", "true"),
            };
            
            CommandUtilities.RunCommand(nameof(GenerateUserSolution), this, ActionArgs);
        }

        if (RunUSharpProjectSetup)
        {
            AddLaunchSettings(ProjectFolder);
            BuildProject(ProjectPath, ProjectFolder);
        }
    }

    private void WriteProjectTemplate(string projectName, string projectFolder)
    {
        Dictionary<string, string> TemplateValues = new Dictionary<string, string>
        {
            { "DOTNET_VERSION", DotNetUtilities.Version }
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

    private void UpdateCsprojDocument(string projectPath, string projectFolder, IReadOnlyList<string>? dependencies, IReadOnlyList<string>? references, bool isEditorOnly, string[] compileIncludeFolder)
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
            ApplyReferences(CsprojDocument, projectFolder, references);
            ApplyCompileIncludeFolder(CsprojDocument, projectFolder, compileIncludeFolder);

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

        csprojDocument.SetProjectProperty("IsPublishable", "true", "'$(UETargetType)' == 'Editor'");
        csprojDocument.SetProjectProperty("IsEditorOnly", "true");
    }

    private void ApplyAnalyzerFlag(XmlDocument csprojDocument)
    {
        if (!ParseParam("SkipIncludeAnalyzers"))
        {
            return;
        }
        
        csprojDocument.SetProjectProperty(SkipIncludeAnalyzersPropertyName, "true");
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
    
    private static void ApplyReferences(XmlDocument csprojDocument, string projectFolder, IReadOnlyList<string>? assemblyDependencies)
    {
        if (assemblyDependencies is null || assemblyDependencies.Count == 0)
        {
            return;
        }

        XmlElement ItemGroup = CsProjectUtilities.GetOrCreateItemGroup(csprojDocument);
        CsProjectUtilities.AddReferences(csprojDocument, ItemGroup, projectFolder, assemblyDependencies);
    }
    
    private static void ApplyCompileIncludeFolder(XmlDocument csprojDocument, string projectFolder, string[] compileIncludeFolders)
    {
        foreach (string Folder in compileIncludeFolders)
        {
            if (string.IsNullOrWhiteSpace(Folder))
            {
                return;
            }

            string Relative = Path.GetRelativePath(projectFolder, Folder);

            XmlElement ItemGroup = CsProjectUtilities.GetOrCreateItemGroup(csprojDocument);

            XmlElement Compile = csprojDocument.CreateElement("Compile");
            Compile.SetAttribute("Include", Path.Combine(Relative, "**", "*.cs"));
            ItemGroup.AppendChild(Compile);
        }
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