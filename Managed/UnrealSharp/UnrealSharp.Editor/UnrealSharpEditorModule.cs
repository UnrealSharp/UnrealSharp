using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Execution;
using Microsoft.Build.Framework;
using UnrealSharp.Core;
using UnrealSharp.Core.Marshallers;
using UnrealSharp.Editor.Interop;
using UnrealSharp.Engine.Core.Modules;
using UnrealSharp.Shared;
using UnrealSharpWeaver;

namespace UnrealSharp.Editor;

// TODO: Automate managed callbacks so we easily can make calls from native to managed.
[StructLayout(LayoutKind.Sequential)]
public unsafe struct FManagedUnrealSharpEditorCallbacks
{
    public delegate* unmanaged<char*, char*, char*, LoggerVerbosity, IntPtr, NativeBool, NativeBool> BuildProjects;
    public delegate* unmanaged<void> ForceManagedGC;
    public delegate* unmanaged<char*, IntPtr, NativeBool> OpenSolution;
    public delegate* unmanaged<char*, void> AddProjectToCollection;

    public FManagedUnrealSharpEditorCallbacks()
    {
        BuildProjects = &ManagedUnrealSharpEditorCallbacks.Build;
        ForceManagedGC = &ManagedUnrealSharpEditorCallbacks.ForceManagedGC;
        OpenSolution = &ManagedUnrealSharpEditorCallbacks.OpenSolution;
        AddProjectToCollection = &ManagedUnrealSharpEditorCallbacks.AddProjectToCollection;
    }
}

class ErrorCollectingLogger : ILogger
{
    public StringBuilder ErrorLog { get; } = new();
    public LoggerVerbosity Verbosity { get; set; }
    public string Parameters { get; set; } = string.Empty;

    public ErrorCollectingLogger(LoggerVerbosity verbosity = LoggerVerbosity.Normal)
    {
        Verbosity = verbosity;
    }

    public void Initialize(IEventSource eventSource)
    {
        eventSource.ErrorRaised += (sender, e) =>
        {
            string fileName = Path.GetFileName(e.File);

            ErrorLog.AppendLine($"{fileName}({e.LineNumber},{e.ColumnNumber}): {e.Message}");
            ErrorLog.AppendLine();
        };
    }

    public void Shutdown()
    {
    }
}

public static class ManagedUnrealSharpEditorCallbacks
{
    private static readonly ProjectCollection ProjectCollection = new();
    private static readonly BuildManager UnrealSharpBuildManager = new("UnrealSharpBuildManager");

    public static void Initialize()
    {
        FUnrealSharpEditorModuleExporter.CallGetProjectPaths(out UnmanagedArray projectPaths);
        List<string> projectPathsList = projectPaths.ToListWithMarshaller(StringMarshaller.FromNative);

        foreach (string projectPath in projectPathsList)
        {
            ProjectCollection.LoadProject(projectPath);
        }
    }

    [UnmanagedCallersOnly]
    public static unsafe NativeBool Build(char* solutionPath,
        char* outputPath,
        char* buildConfiguration,
        LoggerVerbosity loggerVerbosity,
        IntPtr exceptionBuffer,
        NativeBool buildSolution)
    {
        try
        {
            string buildConfigurationString = new string(buildConfiguration);

            if (buildSolution == NativeBool.True)
            {
                ErrorCollectingLogger logger = new ErrorCollectingLogger(loggerVerbosity);
                BuildParameters buildParameters = new(ProjectCollection)
                {
                    Loggers = new List<ILogger> { logger }
                };

                Dictionary<string, string?> globalProperties = new()
                {
                    ["Configuration"] = buildConfigurationString,
                };

                BuildRequestData buildRequest = new BuildRequestData(
                    new string(solutionPath),
                    globalProperties,
                    null,
                    new[] { "Build" },
                    null
                );

                BuildResult result = UnrealSharpBuildManager.Build(buildParameters, buildRequest);
                if (result.OverallResult == BuildResultCode.Failure)
                {
                    throw new Exception(logger.ErrorLog.ToString());
                }
            }

            Weave(outputPath, buildConfigurationString);
        }
        catch (Exception exception)
        {
            StringMarshaller.ToNative(exceptionBuffer, 0, exception.Message);
            return NativeBool.False;
        }

        return NativeBool.True;
    }

    static unsafe void Weave(char* outputPath, string buildConfiguration)
    {
        List<string> assemblyPaths = new();
        foreach (Project? projectFile in ProjectCollection.LoadedProjects)
        {
            string projectName = Path.GetFileNameWithoutExtension(projectFile.FullPath);
            string assemblyPath = Path.Combine(projectFile.DirectoryPath, "bin",
                buildConfiguration, DotNetUtilities.DOTNET_MAJOR_VERSION_DISPLAY, projectName + ".dll");

            assemblyPaths.Add(assemblyPath);
        }

        WeaverOptions weaverOptions = new WeaverOptions
        {
            AssemblyPaths = assemblyPaths,
            OutputDirectory = new string(outputPath),
        };

        Program.Weave(weaverOptions);
    }
    
    [UnmanagedCallersOnly]
    public static void ForceManagedGC()
    {
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced);
        GC.WaitForPendingFinalizers();
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced);
    }

    [UnmanagedCallersOnly]
    public static unsafe NativeBool OpenSolution(char* solutionPath, IntPtr exceptionBuffer)
    {
        try
        {
            string solutionFilePath = new (solutionPath);

            if (!File.Exists(solutionFilePath))
            {
                throw new FileNotFoundException($"Solution not found at path \"{solutionFilePath}\"");
            }

            ProcessStartInfo? startInfo = null;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                startInfo = new ProcessStartInfo("cmd.exe", $"/c start \"\" \"{solutionFilePath}\"");
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                startInfo = new ProcessStartInfo("open", solutionFilePath);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                startInfo = new ProcessStartInfo("xdg-open", solutionFilePath);
            }

            if (startInfo == null)
            {
                throw new PlatformNotSupportedException("Unsupported platform.");
            }

            startInfo.WorkingDirectory = Path.GetDirectoryName(solutionFilePath);
            startInfo.Environment["MsBuildExtensionPath"] = null;
            startInfo.Environment["MSBUILD_EXE_PATH"] = null;
            startInfo.Environment["MsBuildSDKsPath"] = null;

            Process.Start(startInfo);
        }
        catch (Exception exception)
        {
            StringMarshaller.ToNative(exceptionBuffer, 0, exception.Message);
            return NativeBool.False;
        }

        return NativeBool.True;
    }
    
    [UnmanagedCallersOnly]
    public static unsafe void AddProjectToCollection(char* projectPath)
    {
        string projectPathString = new string(projectPath);
        
        if (ProjectCollection.LoadedProjects.All(p => p.FullPath != projectPathString))
        {
            ProjectCollection.LoadProject(projectPathString);
        }
    }
}

public class FUnrealSharpEditor : IModuleInterface
{
    public void StartupModule()
    {
        FManagedUnrealSharpEditorCallbacks callbacks = new FManagedUnrealSharpEditorCallbacks();
        FUnrealSharpEditorModuleExporter.CallInitializeUnrealSharpEditorCallbacks(callbacks);
        ManagedUnrealSharpEditorCallbacks.Initialize();
    }

    public void ShutdownModule()
    {
    }
}