using UnrealSharp;
using UnrealSharp.Attributes;
using UnrealSharp.Core;
using UnrealSharp.Core.Attributes;
using UnrealSharp.CoreUObject;
using UnrealSharp.Engine;
using UnrealSharp.GameplayTags;

namespace TestSourceGen;

[UMultiDelegate]
public delegate void FNullableDelegate(UObject? value, string? text);

[UClass]
public partial class UNullableTypes : UObject
{
    [UProperty] public partial ITestInterface? NullableInterface { get; set; }
    [UProperty] public partial ITestInterface RequiredInterface { get; set; }
    [UProperty] public partial IGameplayTagAssetInterface? NativeInterface { get; set; }
    [UProperty] public partial IList<ITestInterface?> NullableInterfaces { get; set; }
    [UProperty] public partial IList<ITestInterface> RequiredInterfaces { get; set; }
    [UProperty] public partial IDictionary<FName, ITestInterface?> InterfaceMap { get; set; }
    [UProperty] public partial ISet<AActor?> NullableActors { get; set; }
    [UProperty] public partial IList<string?> NullableStrings { get; set; }
    [UProperty, FieldNotify] public partial TArray<AActor?> ObservableActors { get; set; }
    [UProperty, FieldNotify] public partial TMap<FName, AActor?> ObservableActorMap { get; set; }

    [UProperty]
    public UObject? CustomObject
    {
        get => null;
        set { }
    }

    [UFunction(FunctionFlags.BlueprintEvent)]
    public partial UObject? NullableObjectFunction(UObject? value);
    public partial UObject? NullableObjectFunction_Implementation(UObject? value) => value;

    [UFunction(FunctionFlags.BlueprintEvent)]
    public partial ITestInterface? NullableInterfaceFunction(ITestInterface? value);
    public partial ITestInterface? NullableInterfaceFunction_Implementation(ITestInterface? value) => value;

    [UFunction(FunctionFlags.BlueprintEvent)]
    public partial ITestInterface RequiredInterfaceFunction(ITestInterface value);
    public partial ITestInterface RequiredInterfaceFunction_Implementation(ITestInterface value) => value;

    [UFunction(FunctionFlags.BlueprintEvent)]
    public partial IList<ITestInterface> RequiredInterfaceArrayFunction(IList<ITestInterface> values);
    public partial IList<ITestInterface> RequiredInterfaceArrayFunction_Implementation(IList<ITestInterface> values) => values;

    [UFunction(FunctionFlags.BlueprintEvent)]
    public partial IList<AActor?> NullableArrayFunction(IList<AActor?> values);
    public partial IList<AActor?> NullableArrayFunction_Implementation(IList<AActor?> values) => values;

    [UFunction(FunctionFlags.BlueprintEvent)]
    public partial IDictionary<FName, AActor?> NullableMapFunction(IDictionary<FName, AActor?> values);
    public partial IDictionary<FName, AActor?> NullableMapFunction_Implementation(IDictionary<FName, AActor?> values) => values;

    [UFunction(FunctionFlags.BlueprintEvent)]
    public partial string? NullableStringFunction(string? value);
    public partial string? NullableStringFunction_Implementation(string? value) => value;
}

internal static class MapNullability
{
    public static string Lookup(TMap<FName, string> map, FName key) =>
        map.TryGetValue(key, out var value) ? value : string.Empty;

    public static string Lookup(TMapReadOnly<FName, string> map, FName key) =>
        map.TryGetValue(key, out var value) ? value : string.Empty;

    public static string? LookupNullable(TMap<FName, string?> map, FName key) =>
        map.TryGetValue(key, out var value) ? value : null;
}
