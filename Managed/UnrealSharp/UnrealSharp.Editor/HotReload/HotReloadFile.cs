namespace UnrealSharp.Editor.HotReload;

public class HotReloadFile
{
    public HotReloadFile(string fullFilePath, DateTime lastWriteTime)
    {
        FullFilePath = fullFilePath;
        LastWriteTime = lastWriteTime;
    }
    
    public string FullFilePath { get; set; }
    public DateTime LastWriteTime { get; set; }
    
    public bool NeedCompile => !HasCompiled && !IsBeingReloaded;
    public bool HasCompiled => LastCompiledOn.HasValue;
    
    public DateTime? LastCompiledOn { get; set; }
    
    public bool IsBeingReloaded { get; set; }
}