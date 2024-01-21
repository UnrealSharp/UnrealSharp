namespace UnrealSharpWeaver;

[Flags]
public enum StructFlags : ulong
{
    NoFlags				= 0x00000000,	
    Native				= 0x00000001,
    IdenticalNative		= 0x00000002,
    HasInstancedReference= 0x00000004,
    NoExport				= 0x00000008,
    Atomic				= 0x00000010,
    Immutable			= 0x00000020,
    AddStructReferencedObjects = 0x00000040,
    RequiredAPI			= 0x00000200,	
    NetSerializeNative	= 0x00000400,	
    SerializeNative		= 0x00000800,	
    CopyNative			= 0x00001000,	
    IsPlainOldData		= 0x00002000,	
    NoDestructor			= 0x00004000,	
    ZeroConstructor		= 0x00008000,	
    ExportTextItemNative	= 0x00010000,	
    ImportTextItemNative	= 0x00020000,	
    PostSerializeNative  = 0x00040000,
    SerializeFromMismatchedTag = 0x00080000,
    NetDeltaSerializeNative = 0x00100000,
    PostScriptConstruct     = 0x00200000,
    NetSharedSerialization = 0x00400000,
    Trashed = 0x00800000,
    NewerVersionExists = 0x01000000,
    CanEditChange = 0x02000000,
};