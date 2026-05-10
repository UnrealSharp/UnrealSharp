using System.Xml;
using CommandLine;
using Newtonsoft.Json;
using UnrealSharp.Shared;

namespace UnrealSharpBuildTool.Actions;

[Verb("GenerateProjectParameters", aliases: ["GenerateProject"], HelpText = "Generates a new C# project in the specified folder.")]
public struct GenerateProjectParameters
{
    [Option("ProjectFolder", Required = true, HelpText = "The root directory of the Unreal Engine project.")]
    public string ProjectFolder { get; set; }
        
    [Option("ProjectRoot", Required = false, HelpText = "The root directory for the generated C# project.")]
    public string ProjectRoot { get; set; }
        
    [Option("ProjectName", Required = true, HelpText = "The name of the new C# project to generate.")]
    public string ProjectName { get; set; }
        
    [Option("CreateModuleClass", Required = false, Default = false, HelpText = "Whether to create a default module class in the generated project.")]
    public bool CreateModuleClass { get; set; }
        
    [Option("SkipSolutionGeneration", Required = false, Default = false, HelpText = "If true, the generated .csproj file will not be modified to include UnrealSharp properties and dependencies. Defaults to false.")]
    public bool SkipSolutionGeneration { get; set; }
        
    [Option("SkipUSharpProjSetup", Required = false, Default = false, HelpText = "If true, the generated .csproj file will not be modified to include UnrealSharp properties and dependencies. Defaults to false.")]
    public bool SkipUSharpProjSetup { get; set; }

    [Option("EditorOnly", Required = false, Default = false, HelpText = "If true, the generated project will be marked as not publishable. Defaults to false.")]
    public bool EditorOnly { get; set; }
        
    [Option("Dependencies", Required = false, HelpText = "Additional project dependencies to include in the generated .csproj file.")]
    public IEnumerable<string>? Dependencies { get; set; }
}

public static class GenerateProjectAction
{
    public static bool GenerateProject(GenerateProjectParameters parameters)
    {
        string folder = parameters.ProjectFolder;
        string projectRoot = parameters.ProjectRoot;
        
        if (!ContainsUPluginOrUProjectFile(projectRoot))
        {
            throw new InvalidOperationException("Project folder must contain a .uplugin or .uproject file.");
        }
        
        if (folder == projectRoot)
        {
            folder = Path.Combine(folder, CommonUnrealSharpSettings.ScriptDirectoryName);
        }

        string projectName = parameters.ProjectName;
        string csProjFileName = $"{projectName}.csproj";

        string projectFolder = Path.Combine(folder, projectName);
        string projectPath = Path.Combine(projectFolder, csProjFileName);
        
        Dictionary<string, string> templateValues = new Dictionary<string, string>
        {
            { "DOTNET_VERSION", Program.GetVersion() }
        };
        
        TemplateUtilities.WriteTemplateToFile("Csproj", projectName, "csproj", projectFolder, templateValues);

        if (parameters.CreateModuleClass)
        {
            Dictionary<string, string> moduleTemplateValues = new Dictionary<string, string>
            {
                { "MODULE_NAME", projectName }
            };
            
            TemplateUtilities.WriteTemplateToFile("Module", projectName, "cs", projectFolder, moduleTemplateValues);
        }
        
        ModifyModuleFile(projectPath, projectFolder, parameters.Dependencies, parameters.EditorOnly);
        
        if (!parameters.SkipSolutionGeneration)
        {
            GenerateSolutionAction.GenerateSolution(new GenerateSolutionParameters());
        }

        if (parameters.SkipUSharpProjSetup)
        {
            return true;
        }

        AddLaunchSettings(projectFolder);
        BuildProject(projectPath, projectFolder);
        return true;
    }

    private static void ModifyModuleFile(string projectPath, string projectFolder, IEnumerable<string>? dependencies, bool isEditorOnly)
    {
        try
        {
            XmlDocument csprojDocument = new XmlDocument();
            csprojDocument.Load(projectPath);
            csprojDocument.EnsureProjectRoot();

            if (csprojDocument.SelectSingleNode("//ItemGroup") is not XmlElement newItemGroup)
            {
                newItemGroup = csprojDocument.CreateElement("ItemGroup");
                csprojDocument.DocumentElement!.AppendChild(newItemGroup);
            }

            string isPublishable = isEditorOnly ? "false" : "true";
            csprojDocument.SetProjectProperty("IsPublishable", isPublishable);

            string unrealSharpPluginPath = Program.GetUnrealSharpSharedProps();
            string relativeUnrealSharpPath = GetRelativePath(projectFolder, unrealSharpPluginPath);
            csprojDocument.MakeProjectImport(csprojDocument.DocumentElement!, relativeUnrealSharpPath);

            if (dependencies != null)
            {
                foreach (string dependency in dependencies)
                {
                    AddDependency(csprojDocument, newItemGroup, dependency, projectFolder);
                }
            }
            
            csprojDocument.Save(projectPath);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"An error occurred while updating the .csproj file: {ex.Message}", ex);
        }
    }

    private static void AddDependency(XmlDocument doc, XmlElement itemGroup, string dependency, string projectFolder)
    {
        string relativePath = GetRelativePath(projectFolder, dependency);

        XmlElement generatedCode = doc.CreateElement("ProjectReference");
        generatedCode.SetAttribute("Include", relativePath);
        itemGroup.AppendChild(generatedCode);
    }

    public static string GetRelativePath(string basePath, string targetPath)
    {
        Uri baseUri = new Uri(basePath.EndsWith(Path.DirectorySeparatorChar.ToString()) ? basePath : basePath + Path.DirectorySeparatorChar);
        Uri targetUri = new Uri(targetPath);
        Uri relativeUri = baseUri.MakeRelativeUri(targetUri);
        return OperatingSystem.IsWindows() ? Uri.UnescapeDataString(relativeUri.ToString()).Replace('/', '\\') : Uri.UnescapeDataString(relativeUri.ToString());
    }

    static void AddLaunchSettings(string projectFolder)
    {
        string csProjectPath = Path.Combine(Program.GetScriptFolder(), projectFolder);
        string propertiesDirectoryPath = Path.Combine(csProjectPath, "Properties");
        string launchSettingsPath = Path.Combine(propertiesDirectoryPath, "launchSettings.json");

        if (!Directory.Exists(propertiesDirectoryPath))
        {
            Directory.CreateDirectory(propertiesDirectoryPath);
        }

        if (File.Exists(launchSettingsPath))
        {
            return;
        }

        Program.CreateOrUpdateLaunchSettings(launchSettingsPath);
    }

    static void BuildProject(string projectPath, string projectFolder)
    {
        using BuildToolProcess buildProjectProcess = new BuildToolProcess();

        buildProjectProcess.StartInfo.ArgumentList.Add("build");
        buildProjectProcess.StartInfo.ArgumentList.Add(projectPath);
        buildProjectProcess.StartInfo.WorkingDirectory = projectFolder;

        if (!buildProjectProcess.StartBuildToolProcess())
        {
            throw new InvalidOperationException("Failed to build the generated project.");
        }
    }

    static bool ContainsUPluginOrUProjectFile(string folder)
    {
        string[] files = Directory.GetFiles(folder, "*.*", SearchOption.AllDirectories);
        
        foreach (string file in files)
        {
            if (file.EndsWith(".uplugin", StringComparison.OrdinalIgnoreCase) || file.EndsWith(".uproject", StringComparison.OrdinalIgnoreCase))
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
