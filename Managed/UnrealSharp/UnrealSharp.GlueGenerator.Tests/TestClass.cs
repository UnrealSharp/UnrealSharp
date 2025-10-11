﻿using UnrealSharp;
using UnrealSharp.Attributes;
using UnrealSharp.Core;
using UnrealSharp.CoreUObject;
using UnrealSharp.Engine;

namespace TestSourceGen;

[UMultiDelegate]
public delegate void TestDelegate(int intParam, string strParam);

[USingleDelegate]
public delegate void TestDelegate2(int intParam, string strParam);

[UEnum]
public enum ETestEnum : byte
{
    FirstValue,
    SecondValue,
    ThirdValue
}

[UStruct]
public partial struct FTestStruct
{
    [UProperty] public int IntProp { get; set; }
    [UProperty] public string StringProp { get; set; }
    [UProperty] public FVector VectorProp { get; set; }
}

[UInterface]
public interface ITestInterface
{
    [UFunction(FunctionFlags.BlueprintEvent)]
    public void InterfaceFunction(int intParam, string strParam);
    
    [UFunction(FunctionFlags.BlueprintCallable)]
    public void CallInterfaceFunction(int intParam, string strParam);
}

[UClass]
public partial class UTestClass : AActor, ITestInterface
{
    [UProperty] public partial bool BoolProp { get; set; }
    [UProperty] public partial byte ByteProp { get; set; }
    [UProperty] public partial sbyte SByteProp { get; set; }
    [UProperty] public partial short Int16Prop { get; set; }
    [UProperty] public partial ushort UInt16Prop { get; set; }
    [UProperty] public partial int Int32Prop { get; set; }
    [UProperty] public partial uint UInt32Prop { get; set; }
    [UProperty] public partial long Int64Prop { get; set; }
    [UProperty] public partial ulong UInt64Prop { get; set; }
    [UProperty] public partial float FloatProp { get; set; }
    [UProperty] public partial double DoubleProp { get; set; }

    [UProperty] public partial string StringProp { get; set; }
    [UProperty] public partial FName NameProp { get; set; }
    [UProperty] public partial FText TextProp { get; set; }

    [UProperty] public partial FVector VectorProp { get; set; }
    [UProperty] public partial FVector2D Vector2DProp { get; set; }
    [UProperty] public partial FVector4 Vector4Prop { get; set; }
    [UProperty] public partial FRotator RotatorProp { get; set; }
    [UProperty] public partial FQuat QuatProp { get; set; }
    [UProperty] public partial FTransform TransformProp { get; set; }

    [UProperty] public partial FLinearColor LinearColorProp { get; set; }
    [UProperty] public partial FColor ColorProp { get; set; }

    [UProperty] public partial FGuid GuidProp { get; set; }
    [UProperty] public partial FIntPoint IntPointProp { get; set; }
    [UProperty] public partial FIntVector IntVectorProp { get; set; }

    [UProperty] public partial UObject? ObjectProp { get; set; }
    [UProperty] public partial AActor? ActorProp { get; set; }
    [UProperty] public partial UTexture2D? TextureProp { get; set; }
    
    [UProperty] public partial AActor? ObjectPtrActor { get; set; }
    
    [UProperty] public partial TWeakObjectPtr<AActor> WeakActor { get; set; }
    
    [UProperty] public partial TSoftObjectPtr<UTexture2D> SoftTexture { get; set; }
    [UProperty] public partial TSoftClassPtr<AActor> SoftActorClass { get; set; }
    
    [UProperty] public partial UClass? ClassProp { get; set; }
    [UProperty] public partial TSubclassOf<AActor> SubclassActor { get; set; }

    [UProperty] public partial TArray<int> IntArray_TArray { get; set; }
    [UProperty] public partial TArray<FVector> VectorArray_TArray { get; set; }
    [UProperty] public partial TArray<AActor?> ActorArray_TArray { get; set; }
    [UProperty] public partial TArray<TSoftObjectPtr<UTexture2D>> SoftTexArray_TArray { get; set; }
    [UProperty] public partial TArray<TSubclassOf<AActor>> SubclassArray_TArray { get; set; }
    
    [UProperty] public partial IList<int> IntArray_IList { get; set; }
    [UProperty] public partial IList<FVector> VectorArray_IList { get; set; }
    [UProperty] public partial IList<AActor?> ActorArray_IList { get; set; }
    [UProperty] public partial IList<TSoftObjectPtr<UTexture2D>> SoftTexArray_IList { get; set; }
    [UProperty] public partial IList<TSubclassOf<AActor>> SubclassArray_IList { get; set; }
    
    [UProperty] public partial TSet<int> IntSet_TSet { get; set; }
    [UProperty] public partial TSet<FName> NameSet_TSet { get; set; }
    [UProperty] public partial TSet<TSoftObjectPtr<UTexture2D>> SoftTexSet_TSet { get; set; }
    
    [UProperty] public partial ISet<int> IntSet_ISet { get; set; }
    [UProperty] public partial ISet<FName> NameSet_ISet { get; set; }
    [UProperty] public partial ISet<TSoftObjectPtr<UTexture2D>> SoftTexSet_ISet { get; set; }
    
    [UProperty] public partial TMap<string, int> StringToInt_TMap { get; set; }
    [UProperty] public partial TMap<FName, AActor?> NameToActor_TMap { get; set; }
    [UProperty] public partial TMap<string, TSoftObjectPtr<UTexture2D>> stringToSoftTex_TMap { get; set; }
    [UProperty] public partial TMap<TSubclassOf<AActor>, FGuid> SubclassToGuid_TMap { get; set; }
    
    [UProperty] public partial IDictionary<string, int> StringToInt_IDict { get; set; }
    [UProperty] public partial IDictionary<FName, AActor?> NameToActor_IDict { get; set; }
    [UProperty] public partial IDictionary<string, TSoftObjectPtr<UTexture2D>> stringToSoftTex_IDict { get; set; }
    [UProperty] public partial IDictionary<TSubclassOf<AActor>, FGuid> SubclassToGuid_IDict { get; set; }
    [UProperty(PropertyFlags.EditAnywhere)] public partial IDictionary<TSubclassOf<AActor>, FGuid> SubclassToGuid_IDhict { get; set; }
    
    [UProperty] public partial TMulticastDelegate<TestDelegate> MultiDelegateProp { get; set; }
    [UProperty] public partial TDelegate<TestDelegate2> SingleDelegateProp { get; set; }
    [UProperty] public partial TDelegate<TestDelegate2> SingleDelegateProp2 { get; set; }
    
    protected override void BeginPlay_Implementation()
    {
        base.BeginPlay_Implementation();
    }

    [UFunction(FunctionFlags.BlueprintEvent)]
    public partial float TestFunction(int intParam, string strParam);
    public partial float TestFunction_Implementation(int intParam, string strParam)
    {
        return 0f;
    }
    
    [UFunction(FunctionFlags.BlueprintCallable)]
    public void CallTestFunction(int intParam = 7, string strParam = "Hello from C#", ETestEnum test = ETestEnum.FirstValue)
    {
        float result = TestFunction(42, "Hello from C#");
    }

    [UFunction(FunctionFlags.RunOnServer)]
    public partial void ServerFunction(int intParam, string strParam);
    public partial void ServerFunction_Implementation(int intParam, string strParam)
    {
        
    }

    public partial void InterfaceFunction(int intParam, string strParam);
    public partial void InterfaceFunction_Implementation(int intParam, string strParam)
    {
        throw new NotImplementedException();
    }
    
    public void CallInterfaceFunction(int intParam, string strParam)
    {

    }
    
    // Cancellation token is optional.
    [UFunction(FunctionFlags.BlueprintCallable)]
    public async Task<int> SlowAdd(int lhs, int rhs, CancellationToken cancellationToken)
    {
        PrintString($"Commencing the world's slowest addition...");

        await Task.Delay(1000, cancellationToken).ConfigureWithUnrealContext();
        if (cancellationToken.IsCancellationRequested || IsDestroyed) { return default; }

        int result = lhs + rhs;
        PrintString($"{lhs} + {rhs} = {result}!");

        return result;
    }
}



