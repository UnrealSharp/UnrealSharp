using System.Collections.Generic;
using System.IO;
using System.Xml;
using AutomationTool;
using UnrealSharp.Automation.BuildCommands;

namespace UnrealSharp.Automation.Utilities;

public static class NativeAOTUtilities
{
	public static string GetAOTIntermediateDirectory(this BuildCommand buildCommand)
	{
		return Path.Combine(buildCommand.GetUnrealSharpIntermediateDirectory(), "NativeAOT");
	}

	public static void RunAOT(PackageProject command, string publishDirectory, string outputDirectory)
	{
		CreateAOTProject(command, publishDirectory);
		PublishAOT(command, publishDirectory, outputDirectory);
	}
	
	public static void CreateAOTProject(BuildCommand buildCommand, string directory)
	{
		string ProjectName = buildCommand.GetProjectName() + ".AOT";
		
		List<KeyValuePair<string, string>> Arguments = new()
		{
			new KeyValuePair<string, string>("ProjectName", ProjectName),
			new KeyValuePair<string, string>("ProjectFolder", directory)
		};

		List<FileInfo> ProjectFiles = buildCommand.GetManagedProjectFiles();
		
		foreach (FileInfo ProjectFile in ProjectFiles)
		{
			string FileName = Path.Combine(directory, Path.GetFileNameWithoutExtension(ProjectFile.Name) + ".dll");
			Arguments.Add(new KeyValuePair<string, string>("References", FileName));
		}

		CommandUtilities.RunCommand(nameof(GenerateProject), buildCommand, Arguments);
		
		string CsprojPath = Path.Combine(directory, $"{ProjectName}.csproj");
		AddTrimmerRootAssemblies(CsprojPath, ProjectFiles);
		CreateInitializeClass(directory);
	}

	public static void AddTrimmerRootAssemblies(string csprojPath, List<FileInfo> projectFiles)
	{
		XmlDocument Document = new XmlDocument();
		Document.Load(csprojPath);
		
		XmlElement ItemGroup = Document.CreateElement("ItemGroup", Document.DocumentElement!.NamespaceURI);
		Document.DocumentElement.AppendChild(ItemGroup);
		
		foreach (FileInfo ProjectFile in projectFiles)
		{
			XmlElement TrimmerRootAssembly = Document.CreateElement("TrimmerRootAssembly", Document.DocumentElement.NamespaceURI);
			TrimmerRootAssembly.SetAttribute("Include", Path.GetFileNameWithoutExtension(ProjectFile.Name));
			ItemGroup.AppendChild(TrimmerRootAssembly);
		}
		
		Document.Save(csprojPath);
	}

	public static void PublishAOT(PackageProject command, string publishDirectory, string outputDirectory)
	{
		IList<string>? ExtraArguments = new List<string>
		{
			"--runtime=" + DotNetSdkUtilities.GetDotNetRuntimeIdentifier(command.Options.TargetPlatform, command.Options.TargetArchitecture),
			"-p:PublishAot=true",
			$"-p:PublishDir=\"{outputDirectory}\"",
		};
			
		BuildSolution.RunBuild(publishDirectory, command.Options.BuildConfiguration, true, ExtraArguments);
	}

	public static void CreateInitializeClass(string outputDirectory)
	{
	    string FilePath = Path.Combine(outputDirectory, "Main.cs");

	    string SourceCode = """
	        using System.Runtime.InteropServices;
	        using UnrealSharp.Binds;
	        using UnrealSharp.Core;

	        namespace UnrealSharp.Plugins;

	        public static class Main
	        {
	            [UnmanagedCallersOnly(EntryPoint = "InitializeAotRuntime")]
	            private static unsafe NativeBool InitializeAotRuntime(char* workingDirectoryPath, 
	                PluginsCallbacks* pluginCallbacks, 
	                IntPtr bindsCallbacks, 
	                IntPtr managedCallbacks)
	            {
	                try
	                {
	                    AppDomain.CurrentDomain.SetData("APP_CONTEXT_BASE_DIRECTORY", new string(workingDirectoryPath));
	                    
	                    PluginsCallbacks.Initialize(pluginCallbacks);
	                    ManagedCallbacks.Initialize(managedCallbacks);
	                    NativeBinds.Initialize(bindsCallbacks);

	                    Console.WriteLine("UnrealSharp initialized successfully.");
	                    return NativeBool.True;
	                }
	                catch (Exception exception)
	                {
	                    Console.WriteLine(exception);
	                    return NativeBool.False;
	                }
	            }
	        }
	        """;

	    File.WriteAllText(FilePath, SourceCode);
	}
}