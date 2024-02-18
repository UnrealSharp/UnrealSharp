using System.Runtime.InteropServices;

namespace UnrealSharp;

[StructLayout(LayoutKind.Sequential)]
struct InterfaceData
{
    public IntPtr ObjectPointer;
    public IntPtr InterfacePointer;
}