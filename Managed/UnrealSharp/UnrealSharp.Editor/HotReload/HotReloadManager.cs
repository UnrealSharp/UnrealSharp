using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using UnrealSharp.Engine;
using UnrealSharp.Interop;

namespace UnrealSharp.Editor.HotReload;

public class HotReloadManager 
{
    private FileSystemWatcher _fileWatcher;
    private readonly List<HotReloadFile> _hotReloadedFiles = new();
    private readonly string _intermediateDirectory = SystemLibrary.ProjectDirectory + "Intermediate";
    
    private FTimerDelegates.FOnNextTickEvent? _reloadFilesDelegate;
    
    public HotReloadManager()
    {
        FileSystemWatcher fileWatcher = new FileSystemWatcher();
        fileWatcher.Path = SystemLibrary.ProjectDirectory + "Script";
        fileWatcher.IncludeSubdirectories = true;
        fileWatcher.NotifyFilter = NotifyFilters.LastWrite;
        fileWatcher.Changed += OnWatchedFileChange;
        fileWatcher.EnableRaisingEvents = true;
        _fileWatcher = fileWatcher;
    }
    
    void OnWatchedFileChange(object sender, FileSystemEventArgs e)
    {
        if (e.ChangeType != WatcherChangeTypes.Changed || e.Name == null || !e.Name.EndsWith(".cs"))
        {
            return;
        }
        
        AddHotReloadedFile(sender, e);
        
        if (_reloadFilesDelegate != null)
        {
            return;
        }
        
        _reloadFilesDelegate = ReloadFiles;
        IntPtr reloadFilesPtr = Marshal.GetFunctionPointerForDelegate(_reloadFilesDelegate);
        CSTimerExtensions.CallSetTimerForNextTick(reloadFilesPtr);
    }
    
    void AddHotReloadedFile(object sender, FileSystemEventArgs e)
    {
        string fullPath = e.FullPath;
        
        foreach (HotReloadFile hotReloadedFile in _hotReloadedFiles)
        {
            if (hotReloadedFile.FullFilePath != fullPath)
            {
                continue;
            }
            
            hotReloadedFile.LastWriteTime = File.GetLastWriteTime(fullPath);
            return;
        }
        
        HotReloadFile newFile = new HotReloadFile(fullPath, File.GetLastWriteTime(fullPath));
        _hotReloadedFiles.Add(newFile);
    }

    void ReloadFiles()
    {
        LogUnrealSharpEditor.Log("Reloading files...");
        
        _reloadFilesDelegate = null;
        List<HotReloadFile> filesToReload = new List<HotReloadFile>();
        
        foreach (HotReloadFile hotReloadedFile in _hotReloadedFiles)
        {
            if (!hotReloadedFile.NeedCompile)
            {
                continue;
            }
            
            hotReloadedFile.IsBeingReloaded = true;
            filesToReload.Add(hotReloadedFile);
        }
        
        if (filesToReload.Count == 0)
        {
            return;
        }
        
        string assemblyName = Guid.NewGuid().ToString().Replace("-", "");
        string outputAssemblyPath = Path.Combine(_intermediateDirectory, $"{assemblyName}.dll");
        
        List<SyntaxTree> syntaxTrees = new List<SyntaxTree>();
        foreach (HotReloadFile hotReloadedFile in filesToReload)
        {
            string fileContent = File.ReadAllText(hotReloadedFile.FullFilePath);
            SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(fileContent);
            syntaxTrees.Add(syntaxTree);
        }

        // TODO: Add DLL references
        List<MetadataReference> references = new List<MetadataReference>();
            
        CSharpCompilation compilation = CSharpCompilation.Create(assemblyName, syntaxTrees, references, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        using MemoryStream ms = new MemoryStream();
        var result = compilation.Emit(ms);
        
        if (!result.Success)
        {
            foreach (var diagnostic in result.Diagnostics)
            {
                LogUnrealSharpEditor.LogError(diagnostic.ToString());
            }
            return;
        }
        
        File.WriteAllBytes(outputAssemblyPath, ms.ToArray());
    }
}