using System.Diagnostics;
using System.Runtime.InteropServices;
using UnrealSharp.Core;
using UnrealSharp.Core.Marshallers;

namespace UnrealSharp.Editor;

// TODO: Automate managed callbacks so we easily can make calls from native to managed.
[StructLayout(LayoutKind.Sequential)]
public unsafe struct FManagedUnrealSharpEditorCallbacks()
{
    public delegate* unmanaged<IntPtr, NativeBool> RecompileDirtyProjects = &ManagedUnrealSharpEditorCallbacks.RecompileDirtyProjects;
    
    public delegate* unmanaged<char*, char*, IntPtr, void> RecompileChangedFile = &ManagedUnrealSharpEditorCallbacks.RecompileChangedFile;
    public delegate* unmanaged<char*, char*, void> RemoveSourceFile = &ManagedUnrealSharpEditorCallbacks.RemoveSourceFile;
    
    public delegate* unmanaged<void> ForceManagedGC = &ManagedUnrealSharpEditorCallbacks.ForceManagedGc;
    
    public delegate* unmanaged<char*, IntPtr, NativeBool> OpenSolution = &ManagedUnrealSharpEditorCallbacks.OpenSolution;
    
    public delegate* unmanaged<char*, IntPtr, void> LoadSolution = &ManagedUnrealSharpEditorCallbacks.LoadSolution;
    public delegate* unmanaged<char*, IntPtr, void> LoadProject = &ManagedUnrealSharpEditorCallbacks.LoadProject;
    
    public delegate* unmanaged<char*, UnmanagedArray*, void> GetDependentProjects = &ManagedUnrealSharpEditorCallbacks.GetDependentProjects;
}

public static class ManagedUnrealSharpEditorCallbacks
{
    [UnmanagedCallersOnly]
    public static NativeBool RecompileDirtyProjects(IntPtr exceptionBuffer)
    {
        try
        {
            IncrementalCompilationManager.RecompileDirtyProjects();
        }
        catch (InvalidOperationException exception)
        {
            StringMarshaller.ToNative(exceptionBuffer, 0, exception.Message);
            return NativeBool.False;
        }
        catch (Exception exception)
        {
            StringMarshaller.ToNative(exceptionBuffer, 0, exception.ToString());
            return NativeBool.False;
        }
        
        return NativeBool.True;
    }
    
    [UnmanagedCallersOnly]
    public static void ForceManagedGc()
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, true);
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
    public static unsafe void LoadSolution(char* solutionPath, IntPtr callbackPtr)
    {
        string solutionFilePath = new string(solutionPath);
        SolutionManager.LoadSolutionAsync(solutionFilePath, callbackPtr);
    }

    [UnmanagedCallersOnly]
    public static unsafe void LoadProject(char* projectPath, IntPtr callbackPtr)
    {
        string projectFilePath = new string(projectPath);
        SolutionManager.AddProjectAsync(projectFilePath, callbackPtr);
    }
    
    [UnmanagedCallersOnly]
    public static unsafe void RecompileChangedFile(char* projectName, char* filePath, IntPtr exceptionBuffer)
    {
        try
        {
            string projectNameStr = new string(projectName);
            string filePathStr = new string(filePath);
            IncrementalCompilationManager.RecompileChangedFile(projectNameStr, filePathStr);
        }
        catch (Exception exception)
        {
            StringMarshaller.ToNative(exceptionBuffer, 0, exception.Message);
        }
    }
    
    [UnmanagedCallersOnly]
    public static unsafe void RemoveSourceFile(char* projectName, char* filePath)
    {
        string projectNameStr = new string(projectName);
        string filePathStr = new string(filePath);
        IncrementalCompilationManager.RemoveSourceFile(projectNameStr, filePathStr);
    }
    
    [UnmanagedCallersOnly]
    public static unsafe void GetDependentProjects(char* projectName, UnmanagedArray* stringArrayPtr)
    {
        string projectNameStr = new string(projectName);
        IncrementalCompilationManager.GetDependentProjects(projectNameStr, stringArrayPtr);
    }
}