
using UnrealSharp.Attributes;

namespace UnrealSharp.Engine;

[UEnum]
public enum EStreamingSourcePriority : byte 
{
    Highest = 0,
    High = 64,
    Normal = 128,
    Low = 192,
    Lowest = 255,
    Default = Normal,
}