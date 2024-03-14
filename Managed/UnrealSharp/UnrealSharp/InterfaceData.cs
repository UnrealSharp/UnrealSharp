using System.Runtime.InteropServices;

namespace UnrealSharp;

[StructLayout(LayoutKind.Sequential)]
public struct InterfaceData
{
    public IntPtr ObjectPointer;
    public IntPtr InterfacePointer;
}