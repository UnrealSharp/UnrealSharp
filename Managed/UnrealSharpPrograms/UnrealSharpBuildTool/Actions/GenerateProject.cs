using System.Xml;
using CommandLine;
using Newtonsoft.Json;

namespace UnrealSharpBuildTool.Actions;

public static class ProjectGeneration
{
    public struct GenerateProjectParameters
    {
        [Option("Folder", Required = false, HelpText = "The folder where the new project will be created. Defaults to the Script folder.")]
        public string Folder { get; set; }

        [Option("Name", Required = true, HelpText = "The name of the new project. This will be used to create a .csproj file with the same name.")]
        public string Name { get; set; }

        [Option("SkipUSharpProjSetup", Required = false, HelpText = "If set, the project will not be modified to include UnrealSharp references and settings.")]
        public bool SkipUSharpProjSetup { get; set; }
        
        [Option("SkipIncludeProjectGlue", Required = false, HelpText = "If set, the project will not include the ProjectGlue.csproj file, which contains the generated code for UnrealSharp.")]
        public bool SkipIncludeProjectGlue { get; set; }
    }
    
    [Action("GenerateProject", "Generates a new C# project in the Script folder. The project will be a class library with the specified name and will include references to UnrealSharp and UnrealSharp.Core.")]
    public static void GenerateProjectAction(GenerateProjectParameters generateProjectParameters)
    {
        GenerateProject generateProject = new GenerateProject(generateProjectParameters);
        generateProject.Generate();
    }

    public struct GenerateProject
    {
        public GenerateProjectParameters Parameters { get; set; }

        public GenerateProject(GenerateProjectParameters parameters)
        {
            Parameters = parameters;
        }

        public void Generate()
        {
            string folder = Parameters.Folder;

            if (string.IsNullOrEmpty(folder))
            {
                folder = Program.GetScriptFolder();
            }
            else if (!folder.Contains(Program.GetScriptFolder()))
            {
                throw new InvalidOperationException("The project folder must be inside the Script folder.");
            }

            string projectName = Parameters.Name;
            string csProjFileName = $"{projectName}.csproj";

            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }

            string newProject = Path.Combine(folder, projectName);
            string projectPath = Path.Combine(newProject, csProjFileName);

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
                return;
            }

            // dotnet new class lib generates a file named Class1, remove it.
            string myClassFile = Path.Combine(newProject, "Class1.cs");
            if (File.Exists(myClassFile))
            {
                File.Delete(myClassFile);
            }

            string slnPath = Program.GetSolutionFile();
            if (!File.Exists(slnPath))
            {
                GenerateSolutionAction.GenerateSolution();
            }

            if (Parameters.SkipUSharpProjSetup)
            {
                return;
            }

            AddLaunchSettings();
            ModifyCSProjFile();

            string relativePath = Path.GetRelativePath(Program.GetScriptFolder(), projectPath);
            AddProjectToSln(relativePath);
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
                var csprojDocument = new XmlDocument();
                csprojDocument.Load(Parameters.Folder);

                if (csprojDocument.SelectSingleNode("//ItemGroup") is not XmlElement newItemGroup)
                {
                    newItemGroup = csprojDocument.CreateElement("ItemGroup");
                    csprojDocument.DocumentElement!.AppendChild(newItemGroup);
                }

                AppendProperties(csprojDocument);

                AppendReference(csprojDocument, newItemGroup, "UnrealSharp", GetPathToBinaries());
                AppendReference(csprojDocument, newItemGroup, "UnrealSharp.Core", GetPathToBinaries());

                AppendSourceGeneratorReference(csprojDocument, newItemGroup);

                if (!Parameters.SkipIncludeProjectGlue)
                {
                    AppendGeneratedCode(csprojDocument, newItemGroup);
                }

                csprojDocument.Save(Parameters.Folder);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"An error occurred while updating the .csproj file: {ex.Message}", ex);
            }
        }

        private void AddProperty(string name, string value, XmlDocument doc, XmlNode propertyGroup)
        {
            var newProperty = propertyGroup.SelectSingleNode(name);

            if (newProperty == null)
            {
                newProperty = doc.CreateElement(name);
                propertyGroup.AppendChild(newProperty);
            }

            newProperty.InnerText = value;
        }

        private void AppendProperties(XmlDocument doc)
        {
            var propertyGroup = doc.SelectSingleNode("//PropertyGroup");

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
            string directoryPath = Path.GetDirectoryName(Parameters.Folder)!;
            var unrealSharpPath = GetRelativePathToUnrealSharp(directoryPath);
            return Path.Combine(unrealSharpPath, "Binaries", "Managed");
        }

        private void AppendReference(XmlDocument doc, XmlElement itemGroup, string referenceName, string binPath)
        {
            var referenceElement = doc.CreateElement("Reference");
            referenceElement.SetAttribute("Include", referenceName);

            var hintPath = doc.CreateElement("HintPath");
            hintPath.InnerText = Path.Combine(binPath, Program.GetVersion(), referenceName + ".dll");
            referenceElement.AppendChild(hintPath);
            itemGroup.AppendChild(referenceElement);
        }

        private void AppendSourceGeneratorReference(XmlDocument doc, XmlElement itemGroup)
        {
            var sourceGeneratorPath = Path.Combine(GetPathToBinaries(), "UnrealSharp.SourceGenerators.dll");
            var sourceGeneratorReference = doc.CreateElement("Analyzer");
            sourceGeneratorReference.SetAttribute("Include", sourceGeneratorPath);
            itemGroup.AppendChild(sourceGeneratorReference);
        }

        private void AppendGeneratedCode(XmlDocument doc, XmlElement itemGroup)
        {
            var generatedGluePath = Path.Combine(Program.GetScriptFolder(), "ProjectGlue", "ProjectGlue.csproj");
            var relativePath = GetRelativePath(Parameters.Folder, generatedGluePath);

            var generatedCode = doc.CreateElement("ProjectReference");
            generatedCode.SetAttribute("Include", relativePath);
            itemGroup.AppendChild(generatedCode);
        }

        private string GetRelativePathToUnrealSharp(string basePath)
        {
            var targetPath = Path.Combine(basePath, Program.BuildToolOptions.PluginDirectory);
            return GetRelativePath(basePath, targetPath);
        }

        public static string GetRelativePath(string basePath, string targetPath)
        {
            Uri baseUri = new Uri(basePath.EndsWith(Path.DirectorySeparatorChar.ToString()) ? basePath : basePath + Path.DirectorySeparatorChar);
            Uri targetUri = new Uri(targetPath);
            Uri relativeUri = baseUri.MakeRelativeUri(targetUri);
            return OperatingSystem.IsWindows()
                ? Uri.UnescapeDataString(relativeUri.ToString()).Replace('/', '\\')
                : Uri.UnescapeDataString(relativeUri.ToString());
        }

        private void AddLaunchSettings()
        {
            var csProjectPath = Path.Combine(Program.GetScriptFolder(), Parameters.Folder);
            var propertiesDirectoryPath = Path.Combine(csProjectPath, "Properties");
            var launchSettingsPath = Path.Combine(propertiesDirectoryPath, "launchSettings.json");

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
}

public class Root
{
    [JsonProperty("profiles")] public Profiles Profiles { get; set; } = new();
}

public class Profiles
{
    [JsonProperty("UnrealSharp")] public Profile ProfileName { get; set; } = new();
}

public class Profile
{
    [JsonProperty("commandName")] public string CommandName { get; set; } = string.Empty;

    [JsonProperty("executablePath")] public string ExecutablePath { get; set; } = string.Empty;

    [JsonProperty("commandLineArgs")] public string CommandLineArgs { get; set; } = string.Empty;
}