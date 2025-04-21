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

    public FManagedUnrealSharpEditorCallbacks()
    {
        BuildProjects = &ManagedUnrealSharpEditorCallbacks.Build;
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
    public static unsafe NativeBool Build(char* solutionPath, char* outputPath, char* buildConfiguration, LoggerVerbosity loggerVerbosity, IntPtr exceptionBuffer, NativeBool buildSolution)
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
