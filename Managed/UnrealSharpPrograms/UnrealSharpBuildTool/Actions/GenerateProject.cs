using System.Xml;
using Newtonsoft.Json;

namespace UnrealSharpBuildTool.Actions;

public class GenerateProject : BuildToolAction
{
    private string _projectPath = string.Empty;
    private string _projectFolder = string.Empty;
    private string _projectRoot = string.Empty;
    
    bool ContainsUPluginOrUProjectFile(string folder)
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

    public override bool RunAction()
    {
        string folder = Program.GetArgument("NewProjectFolder");
        _projectRoot = Program.GetArgument("ProjectRoot");
        
        if (!ContainsUPluginOrUProjectFile(_projectRoot))
        {
            throw new InvalidOperationException("Project folder must contain a .uplugin or .uproject file.");
        }
        
        if (folder == _projectRoot)
        {
            folder = Path.Combine(folder, "Script");
        }

        string projectName = Program.GetArgument("NewProjectName");
        string csProjFileName = $"{projectName}.csproj";

        _projectFolder = Path.Combine(folder, projectName);
        _projectPath = Path.Combine(_projectFolder, csProjFileName);
        
        TemplateUtilities.WriteTemplateToFile("Csproj", projectName, "csproj", _projectFolder, [Program.GetVersion()]);
        
        ModifyModuleFile();
        
        if (!Program.HasArgument("SkipSolutionGeneration"))
        {
            GenerateSolution generateSolution = new GenerateSolution();
            generateSolution.RunAction();
        }

        if (Program.HasArgument("SkipUSharpProjSetup"))
        {
            return true;
        }

        AddLaunchSettings();
        BuildProject();
        return true;
    }

    private void ModifyModuleFile()
    {
        try
        {
            XmlDocument csprojDocument = new XmlDocument();
            csprojDocument.Load(_projectPath);
            csprojDocument.EnsureProjectRoot();

            if (csprojDocument.SelectSingleNode("//ItemGroup") is not XmlElement newItemGroup)
            {
                newItemGroup = csprojDocument.CreateElement("ItemGroup");
                csprojDocument.DocumentElement!.AppendChild(newItemGroup);
            }

            if (!Program.HasArgument("SkipIncludeProjectGlue"))
            {
                AppendGeneratedCode(csprojDocument, newItemGroup);
            }
            
            XmlElement newPropertyGroup = csprojDocument.MakePropertyGroup(csprojDocument.DocumentElement!);
            XmlElement outputType = csprojDocument.GetOrCreateChild(newPropertyGroup, "IsPublishable");
            
			bool isEditorOnly = Program.GetArgumentBool("EditorOnly");
            outputType.InnerText = (!isEditorOnly).ToString();

            string unrealSharpPluginPath = Program.GetUnrealSharpSharedProps();
            string relativeUnrealSharpPath = GetRelativePath(_projectFolder, unrealSharpPluginPath);
            csprojDocument.MakeProjectImport(csprojDocument.DocumentElement!, relativeUnrealSharpPath);

            foreach (string dependency in Program.GetArguments("Dependency"))
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

    private void AppendGeneratedCode(XmlDocument doc, XmlElement itemGroup)
    {
        string providedGlueName = Program.GetArgument("GlueProjectName");
        string scriptFolder = string.IsNullOrEmpty(_projectRoot) ? Program.GetScriptFolder() : Path.Combine(_projectRoot, "Script");
        string generatedGluePath = Path.Combine(scriptFolder, providedGlueName, $"{providedGlueName}.csproj");
        AddDependency(doc, itemGroup, generatedGluePath);
    }

    private void AddDependency(XmlDocument doc, XmlElement itemGroup, string dependency)
    {
        string relativePath = GetRelativePath(_projectFolder, dependency);

        XmlElement generatedCode = doc.CreateElement("ProjectReference");
        generatedCode.SetAttribute("Include", relativePath);
        itemGroup.AppendChild(generatedCode);
    }

    public static string GetRelativePath(string basePath, string targetPath)
    {
        Uri baseUri = new Uri(basePath.EndsWith(Path.DirectorySeparatorChar.ToString())
                ? basePath
                : basePath + Path.DirectorySeparatorChar);
        Uri targetUri = new Uri(targetPath);
        Uri relativeUri = baseUri.MakeRelativeUri(targetUri);
        return OperatingSystem.IsWindows() ? Uri.UnescapeDataString(relativeUri.ToString()).Replace('/', '\\') : Uri.UnescapeDataString(relativeUri.ToString());
    }

    void AddLaunchSettings()
    {
        string csProjectPath = Path.Combine(Program.GetScriptFolder(), _projectFolder);
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
    
    void BuildProject()
    {
        using BuildToolProcess buildProjectProcess = new BuildToolProcess();

        buildProjectProcess.StartInfo.ArgumentList.Add("build");
        buildProjectProcess.StartInfo.ArgumentList.Add(_projectPath);
        buildProjectProcess.StartInfo.WorkingDirectory = _projectFolder;

        if (!buildProjectProcess.StartBuildToolProcess())
        {
            throw new InvalidOperationException("Failed to build the generated project.");
        }
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
