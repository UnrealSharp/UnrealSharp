using UnrealSharp.GameplayTags;

namespace UnrealSharp.Interop;

[NativeCallbacks]
public static unsafe partial class FGameplayTagContainerExporter
{
    public static delegate* unmanaged<ref GameplayTagContainer, ref FGameplayTag, bool> HasTag;
    public static delegate* unmanaged<ref GameplayTagContainer, ref FGameplayTag, bool> HasTagExact;
    public static delegate* unmanaged<ref GameplayTagContainer, ref GameplayTagContainer, bool> HasAny;
    public static delegate* unmanaged<ref GameplayTagContainer, ref GameplayTagContainer, bool> HasAnyExact;
    public static delegate* unmanaged<ref GameplayTagContainer, ref GameplayTagContainer, bool> HasAll;
    public static delegate* unmanaged<ref GameplayTagContainer, ref GameplayTagContainer, bool> HasAllExact;
    public static delegate* unmanaged<ref GameplayTagContainer, ref GameplayTagContainer, GameplayTagContainer> Filter;
    public static delegate* unmanaged<ref GameplayTagContainer, ref GameplayTagContainer, GameplayTagContainer> FilterExact;
    public static delegate* unmanaged<ref GameplayTagContainer, ref GameplayTagContainer, void> AppendTags;
    public static delegate* unmanaged<ref GameplayTagContainer, ref FGameplayTag, void> AddTag;
    public static delegate* unmanaged<ref GameplayTagContainer, ref FGameplayTag, void> AddTagFast;
    public static delegate* unmanaged<ref GameplayTagContainer, ref FGameplayTag, bool> AddLeafTag;
    public static delegate* unmanaged<ref GameplayTagContainer, ref FGameplayTag, bool, bool> RemoveTag;
    public static delegate* unmanaged<ref GameplayTagContainer, ref GameplayTagContainer, void> RemoveTags;
    public static delegate* unmanaged<ref GameplayTagContainer, void> Reset;
    public static delegate* unmanaged<ref GameplayTagContainer, ref UnmanagedArray, void> ToString;
}