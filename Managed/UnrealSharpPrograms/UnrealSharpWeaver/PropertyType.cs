namespace UnrealSharpWeaver;

// Update this enum in CSMetaData.h if you change this enum
public enum PropertyType : byte
{
    Unknown,

    Bool,

    Int8,
    Int16,
    Int,
    Int64,

    Byte,
    UInt16,
    UInt32,
    UInt64,

    Double,
    Float,

    Enum,

    Interface,
    Struct,
    Class,

    Object,
    DefaultComponent,
    LazyObject,
    WeakObject,

    SoftClass,
    SoftObject,

    Delegate,
    MulticastDelegate,

    Array,
    Map,
    Set,
        
    Str,
    Name,
    Text,

    InternalNativeFixedSizeArray,
    InternalManagedFixedSizeArray
}