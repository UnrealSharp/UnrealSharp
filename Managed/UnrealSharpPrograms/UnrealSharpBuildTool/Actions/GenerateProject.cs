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
        string pluginPath = Program.TryGetArgument("PluginPath");

        if (string.IsNullOrEmpty(folder))
        {
            folder = Program.GetScriptFolder();
        }
        else if (!string.IsNullOrEmpty(pluginPath))
        {
            if (!folder.Contains(Path.Join(pluginPath, "Script")))
            {
                throw new InvalidOperationException("The project folder must be inside the Script folder.");
            }
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
        using BuildToolProcess generateProjectProcess = new BuildToolProcess();

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

        string slnPath = Program.GetSolutionFile();
        if (!File.Exists(slnPath))
        {
            GenerateSolution generateSolution = new GenerateSolution();
            generateSolution.RunAction();
        }

        if (Program.HasArgument("SkipUSharpProjSetup"))
        {
            return true;
        }

        AddLaunchSettings();
        ModifyCSProjFile();

        string relativePath = Path.GetRelativePath(Program.GetScriptFolder(), _projectPath);
        AddProjectToSln(relativePath);

        return true;
    }

    public static void AddProjectToSln(string relativePath)
    {
        AddProjectToSln([relativePath]);
    }

    public static void AddProjectToSln(List<string> relativePaths)
    {
        foreach (var projects in GroupPathsBySolutionFolder(relativePaths))
        {
            using BuildToolProcess addProjectToSln = new BuildToolProcess();
            addProjectToSln.StartInfo.ArgumentList.Add("sln");
            addProjectToSln.StartInfo.ArgumentList.Add("add");

            foreach (string relativePath in projects)
            {
                addProjectToSln.StartInfo.ArgumentList.Add(relativePath);
            }

            addProjectToSln.StartInfo.ArgumentList.Add("-s");
            addProjectToSln.StartInfo.ArgumentList.Add(projects.Key);

            addProjectToSln.StartInfo.WorkingDirectory = Program.GetScriptFolder();
            addProjectToSln.StartBuildToolProcess();
        }
    }

    private static IEnumerable<IGrouping<string, string>> GroupPathsBySolutionFolder(List<string> relativePaths)
    {
        return relativePaths.GroupBy(GetPathRelativeToProject)!;
    }

    private static string GetPathRelativeToProject(string path)
    {
        var fullPath = Path.GetFullPath(path, Program.GetScriptFolder());
        var relativePath = Path.GetRelativePath(Program.GetProjectDirectory(), fullPath);
        var projectDirName = Path.GetDirectoryName(relativePath)!;

        // If we're in the script folder we want these to be in the Script solution folder, otherwise we want these to
        // be in the directory for the plugin itself.
        var containingDirName = Path.GetDirectoryName(projectDirName)!;
        return containingDirName == "Script" ? containingDirName : Path.GetDirectoryName(containingDirName)!;
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
                csprojDocument.DocumentElement!.AppendChild(newItemGroup);
            }

            AppendProperties(csprojDocument);

            AppendPackageReference(csprojDocument, newItemGroup, "LanguageExt.Core", "4.4.9");
            AppendReference(csprojDocument, newItemGroup, "UnrealSharp", GetPathToBinaries());
            AppendReference(csprojDocument, newItemGroup, "UnrealSharp.Core", GetPathToBinaries());

            AppendSourceGeneratorReference(csprojDocument, newItemGroup);

            if (!Program.HasArgument("SkipIncludeProjectGlue"))
            {
                AppendGeneratedCode(csprojDocument, newItemGroup);
            }

            foreach (var dependency in Program.GetArguments("Dependency"))
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

    private void AppendReference(XmlDocument doc, XmlElement itemGroup, string referenceName, string binPath)
    {
        XmlElement referenceElement = doc.CreateElement("Reference");
        referenceElement.SetAttribute("Include", referenceName);

        XmlElement hintPath = doc.CreateElement("HintPath");
        hintPath.InnerText = Path.Combine(binPath, Program.GetVersion(), referenceName + ".dll");
        referenceElement.AppendChild(hintPath);
        itemGroup.AppendChild(referenceElement);
    }

    private void AppendSourceGeneratorReference(XmlDocument doc, XmlElement itemGroup)
    {
        string sourceGeneratorPath = Path.Combine(GetPathToBinaries(), "UnrealSharp.SourceGenerators.dll");
        XmlElement sourceGeneratorReference = doc.CreateElement("Analyzer");
        sourceGeneratorReference.SetAttribute("Include", sourceGeneratorPath);
        itemGroup.AppendChild(sourceGeneratorReference);
    }

    private void AppendPackageReference(XmlDocument doc, XmlElement itemGroup, string packageName, string packageVersion)
    {
        XmlElement packageReference = doc.CreateElement("PackageReference");
        packageReference.SetAttribute("Include", packageName);
        packageReference.SetAttribute("Version", packageVersion);
        itemGroup.AppendChild(packageReference);
    }

    private void AppendGeneratedCode(XmlDocument doc, XmlElement itemGroup)
    {
        string providedGlueName = Program.TryGetArgument("GlueProjectName");
        string glueProjectName = string.IsNullOrEmpty(providedGlueName) ? "ProjectGlue" : providedGlueName;
        string generatedGluePath = Path.Combine(Program.GetScriptFolder(), glueProjectName, $"{glueProjectName}.csproj");
        AddDependency(doc, itemGroup, generatedGluePath);
    }

    private void AddDependency(XmlDocument doc, XmlElement itemGroup, string dependency)
    {
        string relativePath = GetRelativePath(_projectFolder, dependency);

        XmlElement generatedCode = doc.CreateElement("ProjectReference");
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
