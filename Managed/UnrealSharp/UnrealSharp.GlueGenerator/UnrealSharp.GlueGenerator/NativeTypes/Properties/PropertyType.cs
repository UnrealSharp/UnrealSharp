namespace UnrealSharp.GlueGenerator.NativeTypes.Properties;

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
    
    Struct,
    Class,

    Object,
    DefaultComponent,
    WeakObject,

    ScriptInterface,

    SoftClass,
    SoftObject,

    Delegate,
    MulticastInlineDelegate,
    SignatureDelegate,

    Array,
    Map,
    Set,
    Optional,
        
    String,
    Name,
    Text,
};