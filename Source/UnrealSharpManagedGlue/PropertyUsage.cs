using System;

namespace UnrealSharpManagedGlue;

[Flags]
public enum EPropertyUsageFlags : byte
{
    None = 0x00,
    Property = 0x01,
    Parameter = 0x02,
    ReturnValue = 0x04,
    Inner = 0x08,
    StructProperty = 0x10,
    Any = 0xFF,
};
