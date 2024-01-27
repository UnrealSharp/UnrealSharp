using System.Xml;
using Newtonsoft.Json;

namespace UnrealSharpBuildTool.Actions;

public class GenerateProject : BuildToolAction
{
    public override bool RunAction()
    {
        string projectName = Program.GetProjectNameAsManaged();
        string folder = Program.GetScriptFolder();
        string csProjName = $"{projectName}.csproj";

        if (!Directory.Exists(folder))
        {
            Directory.CreateDirectory(folder);
        }
        
        string csProjPath = Path.Combine(folder, csProjName);
        if (!File.Exists(csProjPath))
        {
            string version = Program.GetVersion();
            BuildToolProcess generateProjectProcess = new BuildToolProcess();
        
            // Create a class library.
            generateProjectProcess.StartInfo.ArgumentList.Add("new");
            generateProjectProcess.StartInfo.ArgumentList.Add("classlib");
        
            // Assign project name to the class library.
            generateProjectProcess.StartInfo.ArgumentList.Add("-n");
            generateProjectProcess.StartInfo.ArgumentList.Add(projectName);
        
            // Set the output directory to the current directory.
            generateProjectProcess.StartInfo.ArgumentList.Add("-o");
            generateProjectProcess.StartInfo.ArgumentList.Add(".");
        
            // Set the target framework to the current version.
            generateProjectProcess.StartInfo.ArgumentList.Add("-f");
            generateProjectProcess.StartInfo.ArgumentList.Add(version);
        
            generateProjectProcess.StartInfo.WorkingDirectory = folder;

            if (!generateProjectProcess.StartBuildToolProcess())
            {
                return false;
            }
            
            // dotnet new class lib generates a file named Class1, remove it.
            string myClassFile = Path.Combine(folder, "Class1.cs");
            if (File.Exists(myClassFile))
            {
                File.Delete(myClassFile);
            }
            
            ModifyCSProjFile(folder, csProjName);
            AddLaunchSettings();
        }
        
        if (!File.Exists(Path.Combine(folder, $"{projectName}.sln")))
        {
            BuildToolProcess generateSln = new BuildToolProcess();
        
            // Create a solution.
            generateSln.StartInfo.ArgumentList.Add("new");
            generateSln.StartInfo.ArgumentList.Add("sln");
        
            // Assign project name to the solution.
            generateSln.StartInfo.ArgumentList.Add("-n");
            generateSln.StartInfo.ArgumentList.Add(projectName);
            generateSln.StartInfo.WorkingDirectory = folder;
        
            // Force the creation of the solution.
            generateSln.StartInfo.ArgumentList.Add("--force");
        
            if (!generateSln.StartBuildToolProcess())
            {
                return false;
            }
        
            BuildToolProcess addProjectToSln = new BuildToolProcess();
        
            // Add the project to the solution.
            addProjectToSln.StartInfo.ArgumentList.Add("sln");
            addProjectToSln.StartInfo.ArgumentList.Add("add");
            addProjectToSln.StartInfo.ArgumentList.Add(csProjName);
        
            addProjectToSln.StartInfo.WorkingDirectory = folder;

            if (!addProjectToSln.StartBuildToolProcess())
            {
                return false;
            }
        }
        
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

    private void ModifyCSProjFile(string folder, string csProjName)
    {
        string csProjPath = Path.Combine(folder, csProjName);

        try
        {
            XmlDocument csprojDocument = new XmlDocument();
            csprojDocument.Load(csProjPath);
            
            AppendCopyNugetPackages(csprojDocument);

            XmlElement newItemGroup = CreateItemGroup(csprojDocument);
            AppendUnrealSharpReference(csprojDocument, newItemGroup);
            IncludeGeneratedCode(csprojDocument, newItemGroup);

            csprojDocument.DocumentElement.AppendChild(newItemGroup);
            csprojDocument.Save(csProjPath);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"An error occurred while updating the .csproj file: {ex.Message}", ex);
        }
    }

    private XmlElement CreateItemGroup(XmlDocument doc)
    {
        return doc.CreateElement("ItemGroup");
    }

    private void AppendCopyNugetPackages(XmlDocument doc)
    {
        XmlNode propertyGroup = doc.SelectSingleNode("//PropertyGroup");
        
        if (propertyGroup == null)
        {
            throw new Exception("No property group was found in the csproj file.");
        }
        
        XmlElement copyLocalLockFileAssemblies = doc.CreateElement("CopyLocalLockFileAssemblies");
        copyLocalLockFileAssemblies.InnerText = "true";
        
        propertyGroup.AppendChild(copyLocalLockFileAssemblies);
        
        // Allow unsafe blocks, mostly for generated code.
        XmlElement allowUnsafeBlocks = doc.CreateElement("AllowUnsafeBlocks");
        allowUnsafeBlocks.InnerText = "true"; 
        
        propertyGroup.AppendChild(allowUnsafeBlocks);
    }

    private void AppendUnrealSharpReference(XmlDocument doc, XmlElement itemGroup)
    {
        XmlElement unrealSharpReference = doc.CreateElement("Reference");
        unrealSharpReference.SetAttribute("Include", "UnrealSharp");

        XmlElement newHintPath = doc.CreateElement("HintPath");
        
        string unrealSharpPath = GetUnrealSharpPathRelativeToPlugins();
        string currentVersionStr = Program.GetVersion();
        string dllPath = Path.Combine(unrealSharpPath, "Binaries", "DotNet", currentVersionStr, "UnrealSharp.dll");
        
        newHintPath.InnerText = dllPath;

        unrealSharpReference.AppendChild(newHintPath);
        
        itemGroup.AppendChild(unrealSharpReference);
    }
    
    private void IncludeGeneratedCode(XmlDocument doc, XmlElement itemGroup)
    {
        XmlElement generatedCode = doc.CreateElement("Compile");
        generatedCode.SetAttribute("Include", "obj/generated/**/*.cs");
        itemGroup.AppendChild(generatedCode);
    }
    
    private string GetUnrealSharpPathRelativeToPlugins()
    {
        string basePath = Program.buildToolOptions.ProjectDirectory;
        string targetPath = Path.Combine(basePath, Program.buildToolOptions.PluginDirectory);
        
        Uri baseUri = new Uri(basePath + @"\");
        Uri targetUri = new Uri(targetPath);
        
        Uri relativeUri = baseUri.MakeRelativeUri(targetUri);
        
        string relativePath = Uri.UnescapeDataString(relativeUri.ToString()).Replace('/', '\\');

        return relativePath;
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

        string executablePath = Path.Combine(Program.buildToolOptions.EngineDirectory, "Binaries", "Win64", "UnrealEditor.exe");
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