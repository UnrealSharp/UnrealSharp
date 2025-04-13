using System.Runtime.InteropServices;
using UnrealSharp.Core.Interop;

namespace UnrealSharp.Core;

[StructLayout(LayoutKind.Sequential)]
public struct UnmanagedArray
{
    public IntPtr Data;
    public int ArrayNum;
    public int ArrayMax;
    
    public void Destroy()
    {
        unsafe
        {
            fixed (UnmanagedArray* ptr = &this)
            {
                FScriptArrayExporter.CallDestroy(ptr);
            }
            
            Data = IntPtr.Zero;
            ArrayNum = 0;
            ArrayMax = 0;
        }
    }
}