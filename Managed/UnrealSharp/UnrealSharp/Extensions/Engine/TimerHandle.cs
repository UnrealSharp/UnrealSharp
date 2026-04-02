using System.Runtime.InteropServices;
using UnrealSharp.Attributes;

namespace UnrealSharp.Engine;

public partial struct FTimerHandle
{
    private const uint IndexBits = 24;
    private const uint SerialNumberBits = 40;
    
    private const int MaxIndex = 1 << (int)IndexBits;
    private const ulong MaxSerialNumber = 1UL << (int)SerialNumberBits;
    
    private ulong Handle;

    public FTimerHandle()
    {
        Handle = 0;
    }

    public bool IsValid()
    {
        return Handle != 0;
    }
}