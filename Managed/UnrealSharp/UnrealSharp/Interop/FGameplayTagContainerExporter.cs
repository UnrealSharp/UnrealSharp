using UnrealSharp.GameplayTags;

namespace UnrealSharp.Interop;

[NativeCallbacks]
public unsafe partial class FGameplayTagContainerExporter
{
    public static delegate* unmanaged<ref GameplayTagContainer, ref GameplayTag, bool> HasTag;
    public static delegate* unmanaged<ref GameplayTagContainer, ref GameplayTag, bool> HasTagExact;
    public static delegate* unmanaged<ref GameplayTagContainer, ref GameplayTagContainer, bool> HasAny;
    public static delegate* unmanaged<ref GameplayTagContainer, ref GameplayTagContainer, bool> HasAnyExact;
    public static delegate* unmanaged<ref GameplayTagContainer, ref GameplayTagContainer, bool> HasAll;
    public static delegate* unmanaged<ref GameplayTagContainer, ref GameplayTagContainer, bool> HasAllExact;
    public static delegate* unmanaged<ref GameplayTagContainer, ref GameplayTagContainer, GameplayTagContainer> Filter;
    public static delegate* unmanaged<ref GameplayTagContainer, ref GameplayTagContainer, GameplayTagContainer> FilterExact;
    public static delegate* unmanaged<ref GameplayTagContainer, ref GameplayTagContainer, void> AppendTags;
    public static delegate* unmanaged<ref GameplayTagContainer, ref GameplayTag, void> AddTag;
    public static delegate* unmanaged<ref GameplayTagContainer, ref GameplayTag, void> AddTagFast;
    public static delegate* unmanaged<ref GameplayTagContainer, ref GameplayTag, bool> AddLeafTag;
    public static delegate* unmanaged<ref GameplayTagContainer, ref GameplayTag, bool, bool> RemoveTag;
    public static delegate* unmanaged<ref GameplayTagContainer, ref GameplayTagContainer, void> RemoveTags;
    public static delegate* unmanaged<ref GameplayTagContainer, void> Reset;
    public static delegate* unmanaged<ref GameplayTagContainer, ref UnmanagedArray, void> ToString;
}