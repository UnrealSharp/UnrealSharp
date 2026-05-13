using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using AutomationTool;
using Newtonsoft.Json;
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

        if (!ContainsUPluginOrUProjectFile(ProjectRoot))
        {
            throw new InvalidOperationException("Project folder must contain a .uplugin or .uproject file.");
        }

        if (ProjectFolder == ProjectRoot)
        {
            ProjectFolder = Path.Combine(ProjectFolder, this.GetScriptDirectoryName());
        }

        string CsProjFileName = $"{ProjectName}.csproj";

        string OutputProjectFolder = Path.Combine(ProjectFolder, ProjectName);
        string ProjectPath = Path.Combine(OutputProjectFolder, CsProjFileName);

        Dictionary<string, string> TemplateValues = new Dictionary<string, string>
        {
            { "DOTNET_VERSION", DotNetUtilities.GetVersion() }
        };

        TemplateUtilities.WriteTemplateToFile(this, "Csproj", ProjectName, "csproj", OutputProjectFolder, TemplateValues);

        if (CreateModuleClass)
        {
            Dictionary<string, string> ModuleTemplateValues = new Dictionary<string, string>
            {
                { "MODULE_NAME", ProjectName }
            };

            TemplateUtilities.WriteTemplateToFile(this, "Module", ProjectName, "cs", OutputProjectFolder, ModuleTemplateValues);
        }

        ModifyModuleFile(ProjectPath, OutputProjectFolder, Dependencies, EditorOnly);

        if (!SkipSolutionGeneration)
        {
            GenerateSolution.GenerateManagedSolution(this);
        }

        if (SkipUSharpProjSetup)
        {
            return;
        }

        AddLaunchSettings(OutputProjectFolder);
        BuildProject(ProjectPath, OutputProjectFolder);
    }

    private void ModifyModuleFile(string projectPath, string projectFolder, IEnumerable<string>? dependencies, bool isEditorOnly)
    {
        try
        {
            XmlDocument CsprojDocument = new XmlDocument();
            CsprojDocument.Load(projectPath);
            CsprojDocument.EnsureProjectRoot();

            if (CsprojDocument.SelectSingleNode("//ItemGroup") is not XmlElement NewItemGroup)
            {
                NewItemGroup = CsprojDocument.CreateElement("ItemGroup");
                CsprojDocument.DocumentElement!.AppendChild(NewItemGroup);
            }

            if (isEditorOnly)
            {
                CsprojDocument.SetProjectProperty("IsPublishable", "false", "'$(DisableWithEditor)' == 'true'");
                CsprojDocument.SetProjectProperty("IsEditorOnly", "true");
            }
            
            string UnrealSharpPluginPath = this.GetUnrealSharpSharedPropsPath();
            string RelativeUnrealSharpPath = GetRelativePath(projectFolder, UnrealSharpPluginPath);
            CsprojDocument.MakeProjectImport(CsprojDocument.DocumentElement!, RelativeUnrealSharpPath);

            if (dependencies != null)
            {
                foreach (string Dependency in dependencies)
                {
                    AddDependency(CsprojDocument, NewItemGroup, Dependency, projectFolder);
                }
            }

            CsprojDocument.Save(projectPath);
        }
        catch (Exception Exception)
        {
            throw new InvalidOperationException($"An error occurred while updating the .csproj file: {Exception.Message}", Exception);
        }
    }

    private void AddDependency(XmlDocument doc, XmlElement itemGroup, string dependency, string projectFolder)
    {
        string RelativePath = GetRelativePath(projectFolder, dependency);

        XmlElement GeneratedCode = doc.CreateElement("ProjectReference");
        GeneratedCode.SetAttribute("Include", RelativePath);
        itemGroup.AppendChild(GeneratedCode);
    }

    public string GetRelativePath(string basePath, string targetPath)
    {
        Uri BaseUri = new Uri(basePath.EndsWith(Path.DirectorySeparatorChar.ToString()) ? basePath : basePath + Path.DirectorySeparatorChar);
        Uri TargetUri = new Uri(targetPath);
        Uri RelativeUri = BaseUri.MakeRelativeUri(TargetUri);
        return OperatingSystem.IsWindows() ? Uri.UnescapeDataString(RelativeUri.ToString()).Replace('/', '\\') : Uri.UnescapeDataString(RelativeUri.ToString());
    }

    private void AddLaunchSettings(string projectFolder)
    {
        string CsProjectPath = Path.Combine(this.GetProjectScriptFolder(), projectFolder);
        string PropertiesDirectoryPath = Path.Combine(CsProjectPath, "Properties");
        string LaunchSettingsPath = Path.Combine(PropertiesDirectoryPath, "launchSettings.json");

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

        if (!BuildProjectProcess.StartBuildToolProcess())
        {
            throw new InvalidOperationException("Failed to build the generated project.");
        }
    }

    private static bool ContainsUPluginOrUProjectFile(string folder)
    {
        string[] Files = Directory.GetFiles(folder, "*.*", SearchOption.AllDirectories);

        foreach (string File in Files)
        {
            if (File.EndsWith(".uplugin", StringComparison.OrdinalIgnoreCase) || File.EndsWith(".uproject", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}

public class Root
{
    [JsonProperty("profiles")]
    public Profiles Profiles { get; set; } = new Profiles();
}

public class Profiles
{
    [JsonProperty("UnrealSharp")]
    public Profile ProfileName { get; set; } = new Profile();
}

public class Profile
{
    [JsonProperty("commandName")]
    public string CommandName { get; set; } = string.Empty;

    [JsonProperty("executablePath")]
    public string ExecutablePath { get; set; } = string.Empty;

    [JsonProperty("commandLineArgs")]
    public string CommandLineArgs { get; set; } = string.Empty;
}
