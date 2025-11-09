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

    Interface,
    Struct,
    Class,

    Object,
    ObjectPtr,
    DefaultComponent,
    LazyObject,
    WeakObject,

    ScriptInterface,

    SoftClass,
    SoftObject,

    Delegate,
    MulticastInlineDelegate,
    MulticastSparseDelegate,
    SignatureDelegate,

    Array,
    Map,
    Set,
    Optional,
        
    String,
    Name,
    Text,
	
    GameplayTag,
    GameplayTagContainer,

    InternalNativeFixedSizeArray,
    InternalManagedFixedSizeArray
};