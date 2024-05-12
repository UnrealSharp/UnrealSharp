using System.Runtime.InteropServices;
using UnrealSharp.Interop;

namespace UnrealSharp;

[StructLayout(LayoutKind.Sequential)]
public struct UnmanagedArray
{
    public IntPtr Data;
    public int ArrayNum;
    public int ArrayMax;
    
    public void Destroy()
    {
        FScriptArrayExporter.CallDestroy(ref this);
        Data = IntPtr.Zero;
        ArrayNum = 0;
        ArrayMax = 0;
    }
}