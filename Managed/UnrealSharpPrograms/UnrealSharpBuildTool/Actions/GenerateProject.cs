using System.Xml;
using Newtonsoft.Json;

namespace UnrealSharpBuildTool.Actions;

public class GenerateProject : BuildToolAction
{
    private string _projectPath = string.Empty;
    private string _projectFolder = string.Empty;
    
    public override bool RunAction()
    {
        string folder = Program.TryGetArgument("NewProjectFolder");
        
        if (string.IsNullOrEmpty(folder))
        { 
            folder = Program.GetScriptFolder();
        }
        else if (!folder.Contains(Program.GetScriptFolder()))
        {
            throw new InvalidOperationException("The project folder must be inside the Script folder.");
        }
        
        string projectName = Program.TryGetArgument("NewProjectName");
        string csProjFileName = $"{projectName}.csproj";

        if (!Directory.Exists(folder))
        {
            Directory.CreateDirectory(folder);
        }

        _projectFolder = Path.Combine(folder, projectName);
        _projectPath = Path.Combine(_projectFolder, csProjFileName);
        
        string version = Program.GetVersion();
        BuildToolProcess generateProjectProcess = new BuildToolProcess();
        
        // Create a class library.
        generateProjectProcess.StartInfo.ArgumentList.Add("new");
        generateProjectProcess.StartInfo.ArgumentList.Add("classlib");
        
        // Assign project name to the class library.
        generateProjectProcess.StartInfo.ArgumentList.Add("-n");
        generateProjectProcess.StartInfo.ArgumentList.Add(projectName);
        
        // Set the target framework to the current version.
        generateProjectProcess.StartInfo.ArgumentList.Add("-f");
        generateProjectProcess.StartInfo.ArgumentList.Add(version);
        
        generateProjectProcess.StartInfo.WorkingDirectory = folder;

        if (!generateProjectProcess.StartBuildToolProcess())
        {
            return false;
        }
            
        // dotnet new class lib generates a file named Class1, remove it.
        string myClassFile = Path.Combine(_projectFolder, "Class1.cs");
        if (File.Exists(myClassFile))
        {
            File.Delete(myClassFile);
        }
        
        AddLaunchSettings();
        ModifyCSProjFile();

        string slnPath = Program.GetSolutionFile();
        if (!File.Exists(slnPath))
        {
            GenerateSolution generateSolution = new GenerateSolution();
            generateSolution.RunAction();
        }
        
        string relativePath = Path.GetRelativePath(Program.GetScriptFolder(), _projectPath);
        AddProjectToSln(relativePath);
        
        BuildSolution buildSolution = new BuildSolution();
        if (!buildSolution.RunAction())
        {
            return false;
        }

        WeaveProject weaveProject = new WeaveProject();
        if (!weaveProject.RunAction())
        {
            return false;
        }

        return true;
    }

    public static void AddProjectToSln(string relativePath)
    {
        AddProjectToSln([relativePath]);
    }
    
    public static void AddProjectToSln(List<string> relativePaths)
    {
        BuildToolProcess addProjectToSln = new BuildToolProcess();
        addProjectToSln.StartInfo.ArgumentList.Add("sln");
        addProjectToSln.StartInfo.ArgumentList.Add("add");
        
        foreach (string relativePath in relativePaths)
        {
            addProjectToSln.StartInfo.ArgumentList.Add(relativePath);
        }
        
        addProjectToSln.StartInfo.WorkingDirectory = Program.GetScriptFolder();
        addProjectToSln.StartBuildToolProcess();
    }

    private void ModifyCSProjFile()
    {
        try
        {
            XmlDocument csprojDocument = new XmlDocument();
            csprojDocument.Load(_projectPath);

            if (csprojDocument.SelectSingleNode("//ItemGroup") is not XmlElement newItemGroup)
            {
                newItemGroup = csprojDocument.CreateElement("ItemGroup");
                csprojDocument.DocumentElement.AppendChild(newItemGroup);
            }
            
            AppendProperties(csprojDocument);
            AppendUnrealSharpReference(csprojDocument, newItemGroup);
            AppendSourceGeneratorReference(csprojDocument, newItemGroup);
            AppendGeneratedCode(csprojDocument, newItemGroup);
            
            csprojDocument.Save(_projectPath);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"An error occurred while updating the .csproj file: {ex.Message}", ex);
        }
    }
    
    void AddProperty(string name, string value, XmlDocument doc, XmlNode propertyGroup)
    {
        XmlNode? newProperty = propertyGroup.SelectSingleNode(name);
        
        if (newProperty == null)
        {
            newProperty = doc.CreateElement(name);
            propertyGroup.AppendChild(newProperty);
        }
        
        newProperty.InnerText = value; 
    }

    private void AppendProperties(XmlDocument doc)
    {
        XmlNode? propertyGroup = doc.SelectSingleNode("//PropertyGroup");
        
        if (propertyGroup == null)
        {
            propertyGroup = doc.CreateElement("PropertyGroup");
        }
        
        AddProperty("CopyLocalLockFileAssembliesName", "true", doc, propertyGroup);
        AddProperty("AllowUnsafeBlocks", "true", doc, propertyGroup);
        AddProperty("EnableDynamicLoading", "true", doc, propertyGroup);
    }

    private string GetPathToBinaries()
    {
        string directoryPath = Path.GetDirectoryName(_projectPath)!;
        string unrealSharpPath = GetRelativePathToUnrealSharp(directoryPath);
        return Path.Combine(unrealSharpPath, "Binaries", "Managed");
    }
    
    private bool ElementExists(XmlDocument doc, string elementName, string attributeName, string attributeValue)
    {
        XmlNodeList nodes = doc.GetElementsByTagName(elementName);
        foreach (XmlNode node in nodes)
        {
            if (node.Attributes?[attributeName]?.Value == attributeValue)
            {
                return true;
            }

            if (!string.IsNullOrEmpty(attributeValue) && node.Attributes?[attributeName]?.Value.Contains(attributeValue) == true)
            {
                return true;
            }
        }
        return false;
    }

    private void AppendUnrealSharpReference(XmlDocument doc, XmlElement itemGroup)
    {
        if (ElementExists(doc, "Reference", "Include", "UnrealSharp"))
        {
            return;
        }
        
        XmlElement unrealSharpReference = doc.CreateElement("Reference");
        unrealSharpReference.SetAttribute("Include", "UnrealSharp");

        XmlElement newHintPath = doc.CreateElement("HintPath");
        string binaryPath = GetPathToBinaries();
        
        newHintPath.InnerText = Path.Combine(binaryPath, Program.GetVersion(), "UnrealSharp.dll");
        unrealSharpReference.AppendChild(newHintPath);
        itemGroup.AppendChild(unrealSharpReference);
    }
    
    private void AppendSourceGeneratorReference(XmlDocument doc, XmlElement itemGroup)
    {
        string sourceGeneratorPath = Path.Combine(GetPathToBinaries(), "UnrealSharp.SourceGenerators.dll");
        if (ElementExists(doc, "Analyzer", "Include", sourceGeneratorPath))
        {
            return;
        }
        
        XmlElement sourceGeneratorReference = doc.CreateElement("Analyzer");
        sourceGeneratorReference.SetAttribute("Include", sourceGeneratorPath);
        itemGroup.AppendChild(sourceGeneratorReference);
    }
    
    private void AppendGeneratedCode(XmlDocument doc, XmlElement itemGroup)
    {
        string generatedCodePath = Path.Combine(Program.GetScriptFolder(), "obj", "generated", "**", "*.cs");
        string relativePath = GetRelativePath(_projectFolder, generatedCodePath);
        
        if (ElementExists(doc, "Compile", "Include", relativePath))
        {
            return;
        }
        
        XmlElement generatedCode = doc.CreateElement("Compile");
        generatedCode.SetAttribute("Include", relativePath);
        itemGroup.AppendChild(generatedCode);
    }
    
    private string GetRelativePathToUnrealSharp(string basePath)
    {
        string targetPath = Path.Combine(basePath, Program.BuildToolOptions.PluginDirectory);
        return GetRelativePath(basePath, targetPath);
    }
    
    public static string GetRelativePath(string basePath, string targetPath)
    {
        Uri baseUri = new Uri(basePath.EndsWith(Path.DirectorySeparatorChar.ToString()) 
            ? basePath 
            : basePath + Path.DirectorySeparatorChar);
        Uri targetUri = new Uri(targetPath);
        Uri relativeUri = baseUri.MakeRelativeUri(targetUri);
        string relativePath = Uri.UnescapeDataString(relativeUri.ToString());

        return OperatingSystem.IsWindows() ? Uri.UnescapeDataString(relativeUri.ToString()).Replace('/', '\\') : Uri.UnescapeDataString(relativeUri.ToString());
    }
    
    void AddLaunchSettings()
    {
        string propertiesDirectoryPath = Path.Combine(Program.GetScriptFolder(), "Properties");
        string launchSettingsPath = Path.Combine(propertiesDirectoryPath, "launchSettings.json");

        if (!Directory.Exists(propertiesDirectoryPath))
        {
            Directory.CreateDirectory(propertiesDirectoryPath);
        }
        
        if (File.Exists(launchSettingsPath))
        {
            return;
        }
        
        CreateOrUpdateLaunchSettings(launchSettingsPath);
    }
    
    void CreateOrUpdateLaunchSettings(string launchSettingsPath)
    {
        Root root = new Root();

        string executablePath = string.Empty;
        if (OperatingSystem.IsWindows())
        {
            executablePath = Path.Combine(Program.BuildToolOptions.EngineDirectory, "Binaries", "Win64", "UnrealEditor.exe");
        }
        else if (OperatingSystem.IsMacOS())
        {
            executablePath = Path.Combine(Program.BuildToolOptions.EngineDirectory, "Binaries", "Mac", "UnrealEditor");
        }
        string commandLineArgs = Program.FixPath(Program.GetUProjectFilePath());
        
        // Create a new profile if it doesn't exist
        if (root.Profiles == null)
        {
            root.Profiles = new Profiles();
        }
            
        root.Profiles.ProfileName = new Profile
        {
            CommandName = "Executable",
            ExecutablePath = executablePath,
            CommandLineArgs = $"\"{commandLineArgs}\"",
        };
        
        string newJsonString = JsonConvert.SerializeObject(root, Newtonsoft.Json.Formatting.Indented);
        StreamWriter writer = File.CreateText(launchSettingsPath);
        writer.Write(newJsonString);
        writer.Close();
    }
}

public class Root
{
    [JsonProperty("profiles")]
    public Profiles Profiles { get; set; }
}
public class Profiles
{
    [JsonProperty("UnrealSharp")]
    public Profile ProfileName { get; set; }
}

public class Profile
{
    [JsonProperty("commandName")]
    public string CommandName { get; set; }

    [JsonProperty("executablePath")]
    public string ExecutablePath { get; set; }

    [JsonProperty("commandLineArgs")]
    public string CommandLineArgs { get; set; }
}