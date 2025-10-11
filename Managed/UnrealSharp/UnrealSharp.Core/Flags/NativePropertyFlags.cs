namespace UnrealSharp;

[Flags]
public enum NativePropertyFlags : ulong
{
    None = 0,
    Edit = 0x0000000000000001,
    ConstParm = 0x0000000000000002,
    BlueprintVisible = 0x0000000000000004,
    ExportObject = 0x0000000000000008,
    BlueprintReadOnly = 0x0000000000000010,
    Net = 0x0000000000000020,
    EditFixedSize = 0x0000000000000040,
    Parm = 0x0000000000000080,
    OutParm = 0x0000000000000100,
    ZeroConstructor = 0x0000000000000200,
    ReturnParm = 0x0000000000000400,
    DisableEditOnTemplate = 0x0000000000000800,
    Transient = 0x0000000000002000,
    Config = 0x0000000000004000,
    DisableEditOnInstance = 0x0000000000010000,
    EditConst = 0x0000000000020000,
    GlobalConfig = 0x0000000000040000,
    InstancedReference = 0x0000000000080000,
    DuplicateTransient = 0x0000000000200000,
    SubobjectReference = 0x0000000000400000,
    SaveGame = 0x0000000001000000,
    NoClear = 0x0000000002000000,
    ReferenceParm = 0x0000000008000000,
    BlueprintAssignable = 0x0000000010000000,
    Deprecated = 0x0000000020000000,
    IsPlainOldData = 0x0000000040000000,
    RepSkip = 0x0000000080000000,
    RepNotify = 0x0000000100000000,
    Interp = 0x0000000200000000,
    NonTransactional = 0x0000000400000000,
    EditorOnly = 0x0000000800000000,
    NoDestructor = 0x0000001000000000,
    AutoWeak = 0x0000004000000000,
    ContainsInstancedReference = 0x0000008000000000,
    AssetRegistrySearchable = 0x0000010000000000,
    SimpleDisplay = 0x0000020000000000,
    AdvancedDisplay = 0x0000040000000000,
    Protected = 0x0000080000000000,
    BlueprintCallable = 0x0000100000000000,
    BlueprintAuthorityOnly = 0x0000200000000000,
    TextExportTransient = 0x0000400000000000,
    NonPIEDuplicateTransient = 0x0000800000000000,
    ExposeOnSpawn = 0x0001000000000000,
    PersistentInstance = 0x0002000000000000,
    UObjectWrapper = 0x0004000000000000,
    HasGetValueTypeHash = 0x0008000000000000,
    NativeAccessSpecifierPublic = 0x0010000000000000,
    NativeAccessSpecifierProtected = 0x0020000000000000,
    NativeAccessSpecifierPrivate = 0x0040000000000000,
    SkipSerialization = 0x0080000000000000,

    /* Combination flags */

    NativeAccessSpecifiers = NativeAccessSpecifierPublic | NativeAccessSpecifierProtected | NativeAccessSpecifierPrivate,

    ParmFlags = Parm | OutParm | ReturnParm | ReferenceParm | ConstParm,
    PropagateToArrayInner = ExportObject | PersistentInstance | InstancedReference | ContainsInstancedReference | Config | EditConst | Deprecated | EditorOnly | AutoWeak | UObjectWrapper,
    PropagateToMapValue = ExportObject | PersistentInstance | InstancedReference | ContainsInstancedReference | Config | EditConst | Deprecated | EditorOnly | AutoWeak | UObjectWrapper | Edit,
    PropagateToMapKey = ExportObject | PersistentInstance | InstancedReference | ContainsInstancedReference | Config | EditConst | Deprecated | EditorOnly | AutoWeak | UObjectWrapper | Edit,
    PropagateToSetElement = ExportObject | PersistentInstance | InstancedReference | ContainsInstancedReference | Config | EditConst | Deprecated | EditorOnly | AutoWeak | UObjectWrapper | Edit,

    /** the flags that should never be set on interface properties */
    InterfaceClearMask = ExportObject | InstancedReference | ContainsInstancedReference,

    /** all the properties that can be stripped for final release console builds */
    DevelopmentAssets = EditorOnly,

    /** all the properties that should never be loaded or saved */
    ComputedFlags = IsPlainOldData | NoDestructor | ZeroConstructor | HasGetValueTypeHash,

    EditDefaultsOnly = Edit | DisableEditOnInstance,
    EditInstanceOnly = Edit | DisableEditOnTemplate,
    EditAnywhere = Edit,
    
    VisibleAnywhere = BlueprintVisible | BlueprintReadOnly,
    VisibleDefaultsOnly = BlueprintVisible | BlueprintReadOnly | DisableEditOnInstance,
    VisibleInstanceOnly = BlueprintVisible | BlueprintReadOnly | DisableEditOnTemplate,
    
    BlueprintReadWrite = BlueprintVisible | Edit,

    AllFlags = 0xFFFFFFFFFFFFFFFF
}