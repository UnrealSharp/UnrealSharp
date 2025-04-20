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
    public delegate* unmanaged<char*, char*, IntPtr, NativeBool, NativeBool> BuildProjects;

    public FManagedUnrealSharpEditorCallbacks()
    {
        BuildProjects = &UnrealSharpEditorCallbacks.BuildProjects;
    }
}

class ErrorCollectingLogger : ILogger
{
    public StringBuilder ErrorLog { get; } = new();
    public LoggerVerbosity Verbosity { get; set; } = LoggerVerbosity.Normal;
    public string Parameters { get; set; } = string.Empty;
    
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
        ErrorLog.Clear();
    }
}

public static class UnrealSharpEditorCallbacks
{
    private static readonly ProjectCollection ProjectCollection = new();
    private static readonly BuildManager UnrealSharpBuildManager = new("UnrealSharpBuildManager");
    private static readonly List<string> AssemblyPaths = new();
    
    public static void Initialize()
    {
        FUnrealSharpEditorModuleExporter.CallGetProjectPaths(out UnmanagedArray projectPaths);
        List<string> projectPathsList = projectPaths.ToListWithMarshaller(StringMarshaller.FromNative);
        
        foreach (string projectPath in projectPathsList)
        {
            ProjectCollection.LoadProject(projectPath);
        }
        
        foreach (Project? projectFile in ProjectCollection.LoadedProjects)
        {
            string projectName = Path.GetFileNameWithoutExtension(projectFile.FullPath);
            string assemblyPath = Path.Combine(projectFile.DirectoryPath, "bin", 
                "Debug", DotNetUtilities.DOTNET_MAJOR_VERSION_DISPLAY, projectName + ".dll");
            
            AssemblyPaths.Add(assemblyPath);
        }
    }
    
    [UnmanagedCallersOnly]
    public static unsafe NativeBool BuildProjects(char* solutionPath, char* outputPath, IntPtr exceptionBuffer, NativeBool buildSolution)
    {
        try
        {
            if (buildSolution == NativeBool.True)
            {
                BuildParameters buildParameters = new(ProjectCollection)
                {
                    Loggers = new List<ILogger> { new ErrorCollectingLogger() }
                };
    
                BuildRequestData buildRequest = new BuildRequestData(
                    new string(solutionPath),
                    new Dictionary<string, string?>(),
                    null,
                    new[] { "Build" },
                    null
                );
            
                BuildResult result = UnrealSharpBuildManager.Build(buildParameters, buildRequest);
                if (result.OverallResult == BuildResultCode.Failure)
                {
                    foreach (ILogger logger in buildParameters.Loggers)
                    {
                        if (logger is ErrorCollectingLogger errorLogger)
                        {
                            throw new Exception(errorLogger.ErrorLog.ToString());
                        }
                    }
                
                    throw new Exception("Build failed with no error log available.");
                }
            }
            
            Weave(outputPath);
        }
        catch (Exception exception)
        {
            StringMarshaller.ToNative(exceptionBuffer, 0, exception.Message);
            return NativeBool.False;
        }
        
        return NativeBool.True;
    }

    static unsafe void Weave(char* outputPath)
    {
        WeaverOptions weaverOptions = new WeaverOptions
        {
            AssemblyPaths = AssemblyPaths,
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
        UnrealSharpEditorCallbacks.Initialize();
    }

    public void ShutdownModule()
    {

    }
}
