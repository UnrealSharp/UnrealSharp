using System.Collections.ObjectModel;
using System.Text;
using CommandLine;

namespace UnrealSharpBuildTool.Actions;

[Verb("PackageProjectParameters", aliases: ["PackageProject"], HelpText = "Packages the project. This will create a self-contained package in a archived directory.")]
public struct PackageProjectParameters
{
    [Option("ArchiveDirectory", Required = true, HelpText = "The directory base directory where the packaged game is located")]
    public string ArchiveDirectory { get; set; }
        
    [Option("TargetPlatform", Required = true, HelpText = "The target platform for the package")]
    public TargetPlatform TargetPlatform { get; set; }

    [Option("TargetArchitecture", Required = true, HelpText = "The target architecture for the package")]
    public TargetArchitecture TargetArchitecture { get; set; }
         
    [Option("BuildConfig", Required = false, HelpText = "The build configuration for the package (Debug, Release, etc.)")]
    public TargetConfiguration BuildConfig { get; set; }
        
    [Option("NativeAOT", Required = false, HelpText = "Enable Native AOT compilation. Will overwrite the BuildConfig to Release if set to true.")]
    public bool NativeAOT { get; set; }
        
    [Option("IncludeDebugSymbols", Required = false, HelpText = "Include debug symbols in the package. Defaults to false.")]
    public bool IncludeDebugSymbols { get; set; }
        
    [Option("UETargetType", Required = false, HelpText = "The type of Unreal Engine target (Editor, Game, etc.). Required for the Publish action.")]
    public string UETargetType { get; set; }
}

public static class PackageProjectAction
{
    public static void PackageProject(PackageProjectParameters parameters)
    {
        if (!Directory.Exists(parameters.ArchiveDirectory))
        {
            throw new DirectoryNotFoundException(parameters.ArchiveDirectory);
        }
        
        Program.CopyGlobalJson();

        string buildOutput = Program.GetIntermediateBuildPathForPlatform(
            parameters.TargetArchitecture,
            parameters.TargetPlatform,
            parameters.BuildConfig);
        
        string bindingsPath = Path.Combine(BuildToolOptions.Instance.PluginDirectory, "Managed", "UnrealSharp");
        string packageOutputFolder = Path.Combine(BuildToolOptions.Instance.PluginDirectory, "Intermediate", "Build", "Managed");

        string UETargetType = parameters.UETargetType;
        
        if (string.IsNullOrEmpty(UETargetType))
        {
            throw new Exception("UETargetType argument is required for the Publish action.");
        }
        
        Collection<string> extraArguments =
        [
            "--self-contained",
            
            "--runtime",
            "win-x64",
            
			"-p:DisableWithEditor=true",
            "-p:GenerateDocumentationFile=false",
            
            $"-p:UETargetType={UETargetType}",
            
            $"-p:PublishDir=\"{parameters.ArchiveDirectory}\"",
            $"-p:OutputPath=\"{packageOutputFolder}\"",
        ];

        bool selfContained = !parameters.NativeAOT;
        if (selfContained)
        {
            extraArguments.Add("--self-contained");
        }
        
        BuildSolutionParameters buildParameters = new BuildSolutionParameters
        {
            ExtraArguments = extraArguments,
            BuildConfig = parameters.BuildConfig,
            Publish = true,
            Folders = [bindingsPath, Program.GetScriptFolder()],
        };
        
        BuildSolutionAction.BuildSolution(buildParameters);
        
        if (parameters.NativeAOT)
        {
            PublishAsAot(parameters, packageOutputFolder, buildOutput);
        }
    }

    private static void PublishAsAot(PackageProjectParameters parameters, string output, string outputPath)
    {
        string[] dlls = Directory.GetFiles(output, "*.dll", SearchOption.AllDirectories);

        List<FileInfo> userWeavedAssemblies = new List<FileInfo>();
        foreach (string dll in dlls)
        {
            FileInfo fileInfo = new FileInfo(dll);
            userWeavedAssemblies.Add(fileInfo);
        }

        dlls = Directory.GetFiles(outputPath, "*.dll", SearchOption.AllDirectories);
        List<FileInfo> bindings = new List<FileInfo>(dlls.Length);
        foreach (string dll in dlls)
        {
            FileInfo fileInfo = new FileInfo(dll);

            if (userWeavedAssemblies.Any(x => x.Name == fileInfo.Name))
            {
                // This is a user weaved assembly, skip it
                continue;
            }
            
            bindings.Add(fileInfo);
        }
        
        BuildResponseFile(parameters, userWeavedAssemblies, bindings, outputPath);
    }

    public static void BuildResponseFile(PackageProjectParameters parameters, List<FileInfo> userAssemblies, List<FileInfo> bindings, string outputPath)
    {
        TargetConfiguration targetConfiguration = parameters.BuildConfig;
        string targetPlatform = parameters.TargetPlatform.GetTargetPlatform();
        
        string runtimeIdentifier = DotNetSdk.GetIdentifier(parameters.TargetPlatform, parameters.TargetArchitecture);
        
        string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        string ilcRoot = Path.Combine(userProfile, $".nuget\\packages\\runtime.{runtimeIdentifier}.microsoft.dotnet.ilcompiler\\9.0.4");
        
        if (!Directory.Exists(ilcRoot))
        {
            throw new DirectoryNotFoundException($"ILC root directory not found: {ilcRoot}");
        }
        
        StringBuilder responseFileBuilder = new StringBuilder();
        
        foreach (FileInfo userAssembly in userAssemblies)
        {
            responseFileBuilder.AppendLine($"\"{userAssembly.FullName}\"");
        }
        
        string ilcOutputPath = Path.Combine(outputPath, "ILC");
        string outputObjFilePath = Path.Combine(ilcOutputPath, "AOT" + ".obj");
        
        responseFileBuilder.AppendLine($"-o:{outputObjFilePath}");
        
        List<string> referenceFiles = new List<string>();
        referenceFiles.AddRange(userAssemblies.Select(x => x.FullName));
        referenceFiles.AddRange(bindings.Select(x => x.FullName));
        referenceFiles.AddRange(Directory.GetFiles(Path.Combine(ilcRoot, "sdk"), "*.dll"));
        referenceFiles.AddRange(Directory.GetFiles(Path.Combine(ilcRoot, "framework"), "*.dll"));
        
        foreach (string referenceFile in referenceFiles)
        {
            responseFileBuilder.AppendLine("-r:" + referenceFile);
        }
        
        responseFileBuilder.AppendLine("--targetos:" + targetPlatform);
        responseFileBuilder.AppendLine("--targetarch:" + parameters.TargetArchitecture.GetTargetArchitecture());
        responseFileBuilder.AppendLine("--dehydrate");
        
        if (targetConfiguration != TargetConfiguration.Debug)
        {
            responseFileBuilder.AppendLine("-O");
        }

        if (targetConfiguration == TargetConfiguration.Release)
        {
            responseFileBuilder.AppendLine("--Ot");
        }
        
        if (targetConfiguration != TargetConfiguration.Release)
        {
            responseFileBuilder.AppendLine("-g");
        }
        
        responseFileBuilder.AppendLine("--nativelib");
        responseFileBuilder.AppendLine("--exportsfile:" + Path.Combine(ilcOutputPath, "DotnetAot.exports.def"));
        responseFileBuilder.AppendLine("--export-unmanaged-entrypoints");
        
        responseFileBuilder.AppendLine("--initassembly:System.Private.CoreLib");
        responseFileBuilder.AppendLine("--initassembly:System.Private.StackTraceMetadata");
        responseFileBuilder.AppendLine("--initassembly:System.Private.TypeLoader");
        responseFileBuilder.AppendLine("--initassembly:System.Private.Reflection.Execution");
        
        responseFileBuilder.AppendLine("--directpinvoke:System.Globalization.Native");
        responseFileBuilder.AppendLine("--directpinvoke:System.IO.Compression.Native");
        
        if (targetPlatform == "win")
        {
            string windowsAPIsPath = Path.Combine(ilcRoot, "build", "WindowsAPIs.txt");
            
            if (!File.Exists(windowsAPIsPath))
            {
                throw new FileNotFoundException($"Windows APIs file not found at: {windowsAPIsPath}");
            }
            
            responseFileBuilder.AppendLine($"--directpinvokelist:{windowsAPIsPath}");
        }

        responseFileBuilder.AppendLine("--feature:Microsoft.Extensions.DependencyInjection.VerifyOpenGenericServiceTrimmability=true");
        responseFileBuilder.AppendLine("--feature:System.ComponentModel.DefaultValueAttribute.IsSupported=false");
        responseFileBuilder.AppendLine("--feature:System.ComponentModel.Design.IDesignerHost.IsSupported=false");
        responseFileBuilder.AppendLine("--feature:System.ComponentModel.TypeConverter.EnableUnsafeBinaryFormatterInDesigntimeLicenseContextSerialization=false");
        responseFileBuilder.AppendLine("--feature:System.ComponentModel.TypeDescriptor.IsComObjectDescriptorSupported=false");
        responseFileBuilder.AppendLine("--feature:System.Diagnostics.Tracing.EventSource.IsSupported=false");
        responseFileBuilder.AppendLine("--feature:System.Reflection.Metadata.MetadataUpdater.IsSupported=false");
        responseFileBuilder.AppendLine("--feature:System.Resources.ResourceManager.AllowCustomResourceTypes=false");
        responseFileBuilder.AppendLine("--feature:System.Resources.UseSystemResourceKeys=false");
        responseFileBuilder.AppendLine("--feature:System.Runtime.CompilerServices.RuntimeFeature.IsDynamicCodeSupported=false");
        responseFileBuilder.AppendLine("--feature:System.Runtime.InteropServices.BuiltInComInterop.IsSupported=false");
        responseFileBuilder.AppendLine("--feature:System.Runtime.InteropServices.EnableConsumingManagedCodeFromNativeHosting=false");
        responseFileBuilder.AppendLine("--feature:System.Runtime.InteropServices.EnableCppCLIHostActivation=false");
        responseFileBuilder.AppendLine("--feature:System.Runtime.InteropServices.Marshalling.EnableGeneratedComInterfaceComImportInterop=false");
        responseFileBuilder.AppendLine("--feature:System.Runtime.Serialization.EnableUnsafeBinaryFormatterSerialization=false");
        responseFileBuilder.AppendLine("--feature:System.StartupHookProvider.IsSupported=false");
        responseFileBuilder.AppendLine("--feature:System.Text.Encoding.EnableUnsafeUTF7Encoding=false");
        responseFileBuilder.AppendLine("--feature:System.Text.Json.JsonSerializer.IsReflectionEnabledByDefault=false");
        responseFileBuilder.AppendLine("--feature:System.Threading.Thread.EnableAutoreleasePool=false");
        responseFileBuilder.AppendLine("--feature:System.Threading.ThreadPool.UseWindowsThreadPool=true");
        responseFileBuilder.AppendLine("--feature:System.Linq.Expressions.CanEmitObjectArrayDelegate=false");

        responseFileBuilder.AppendLine("--runtimeknob:Microsoft.Extensions.DependencyInjection.VerifyOpenGenericServiceTrimmability=true");
        responseFileBuilder.AppendLine("--runtimeknob:System.ComponentModel.DefaultValueAttribute.IsSupported=false");
        responseFileBuilder.AppendLine("--runtimeknob:System.ComponentModel.Design.IDesignerHost.IsSupported=false");
        responseFileBuilder.AppendLine("--runtimeknob:System.ComponentModel.TypeConverter.EnableUnsafeBinaryFormatterInDesigntimeLicenseContextSerialization=false");
        responseFileBuilder.AppendLine("--runtimeknob:System.ComponentModel.TypeDescriptor.IsComObjectDescriptorSupported=false");
        responseFileBuilder.AppendLine("--runtimeknob:System.Diagnostics.Tracing.EventSource.IsSupported=false");
        responseFileBuilder.AppendLine("--runtimeknob:System.Reflection.Metadata.MetadataUpdater.IsSupported=false");
        responseFileBuilder.AppendLine("--runtimeknob:System.Resources.ResourceManager.AllowCustomResourceTypes=false");
        responseFileBuilder.AppendLine("--runtimeknob:System.Resources.UseSystemResourceKeys=false");
        responseFileBuilder.AppendLine("--runtimeknob:System.Runtime.CompilerServices.RuntimeFeature.IsDynamicCodeSupported=false");
        responseFileBuilder.AppendLine("--runtimeknob:System.Runtime.InteropServices.BuiltInComInterop.IsSupported=false");
        responseFileBuilder.AppendLine("--runtimeknob:System.Runtime.InteropServices.EnableConsumingManagedCodeFromNativeHosting=false");
        responseFileBuilder.AppendLine("--runtimeknob:System.Runtime.InteropServices.EnableCppCLIHostActivation=false");
        responseFileBuilder.AppendLine("--runtimeknob:System.Runtime.InteropServices.Marshalling.EnableGeneratedComInterfaceComImportInterop=false");
        responseFileBuilder.AppendLine("--runtimeknob:System.Runtime.Serialization.EnableUnsafeBinaryFormatterSerialization=false");
        responseFileBuilder.AppendLine("--runtimeknob:System.StartupHookProvider.IsSupported=false");
        responseFileBuilder.AppendLine("--runtimeknob:System.Text.Encoding.EnableUnsafeUTF7Encoding=false");
        responseFileBuilder.AppendLine("--runtimeknob:System.Text.Json.JsonSerializer.IsReflectionEnabledByDefault=false");
        responseFileBuilder.AppendLine("--runtimeknob:System.Threading.Thread.EnableAutoreleasePool=false");
        responseFileBuilder.AppendLine("--runtimeknob:System.Threading.ThreadPool.UseWindowsThreadPool=true");
        responseFileBuilder.AppendLine("--runtimeknob:System.Linq.Expressions.CanEmitObjectArrayDelegate=false");
        
        responseFileBuilder.AppendLine("--stacktracedata");
        responseFileBuilder.AppendLine("--scanreflection");

        responseFileBuilder.AppendLine("--nowarn:\"1701;1702;IL2121;1701;1702\"");
        responseFileBuilder.AppendLine("--warnaserr:\";NU1605;SYSLIB0011\"");

        responseFileBuilder.AppendLine("--singlewarn");
        
        foreach (FileInfo userAssembly in userAssemblies)
        {
            string filenameWithoutExtension = Path.GetFileNameWithoutExtension(userAssembly.Name);
            responseFileBuilder.AppendLine($"--nosinglewarnassembly:{filenameWithoutExtension}");
        }
        
        responseFileBuilder.AppendLine("--nosinglewarnassembly:ProjectGlue");
        responseFileBuilder.AppendLine("--resilient");

        responseFileBuilder.AppendLine("--generateunmanagedentrypoints:System.Private.CoreLib");
        responseFileBuilder.AppendLine("--feature:System.Diagnostics.Debugger.IsSupported=false");
        
        string ilcResponseFile = Path.Combine(ilcOutputPath, "AOT.ilc.rsp");
        
        Directory.CreateDirectory(ilcOutputPath);
        File.WriteAllText(ilcResponseFile, responseFileBuilder.ToString());
        
        string ilcExePath = Path.Combine(ilcRoot, "tools", "ilc.exe");
        
        if (!File.Exists(ilcExePath))
        {
            throw new FileNotFoundException($"ILC executable not found at: {ilcExePath}");
        }
        
        Console.WriteLine("Creating response file for Native AOT compilation...");
        
        BuildToolProcess ilcProcess = new BuildToolProcess(ilcExePath);
        ilcProcess.StartInfo.ArgumentList.Add($"@{ilcResponseFile}");
        ilcProcess.StartBuildToolProcess();
    }
}
