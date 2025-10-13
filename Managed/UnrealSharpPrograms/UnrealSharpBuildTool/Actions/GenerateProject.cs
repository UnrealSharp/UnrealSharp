﻿using System.Xml;
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
        string folder = Program.TryGetArgument("NewProjectFolder");
        _projectRoot = Program.TryGetArgument("ProjectRoot");
        
        if (!ContainsUPluginOrUProjectFile(_projectRoot))
        {
            throw new InvalidOperationException("Project folder must contain a .uplugin or .uproject file.");
        }
        
        if (folder == _projectRoot)
        {
            folder = Path.Combine(folder, "Script");
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
        
        CreateDirectoryBuildPropsFile emitter = new CreateDirectoryBuildPropsFile();
        emitter.RunAction();

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
        foreach (IGrouping<string, string> projects in GroupPathsBySolutionFolder(relativePaths))
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

            if (!Program.HasArgument("SkipIncludeProjectGlue"))
            {
                AppendGeneratedCode(csprojDocument, newItemGroup);
            }

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
        string providedGlueName = Program.TryGetArgument("GlueProjectName");
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
