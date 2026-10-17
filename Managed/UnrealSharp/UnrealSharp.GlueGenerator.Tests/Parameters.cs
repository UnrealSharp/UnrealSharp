using UnrealSharp.Attributes;
using UnrealSharp.Core;
using UnrealSharp.CoreUObject;
using UnrealSharp.Engine;
using UnrealSharp.GameplayAbilities;

namespace TestSourceGen;

[USingleDelegate]
public delegate void FRefParameterDelegate(ref int value);

[UClass]
public partial class UParameterTest : UGameplayModMagnitudeCalculation
{
    [UFunction(FunctionFlags.BlueprintEvent)]
    public partial void ReadValue(int input, out int value);

    public partial void ReadValue_Implementation(int input, out int value)
    {
        value = input + 10;
    }

    [UFunction(FunctionFlags.BlueprintEvent)]
    public partial void ReadValues(int input, out int firstValue, out int secondValue);

    public partial void ReadValues_Implementation(int input, out int firstValue, out int secondValue)
    {
        firstValue = input + 10;
        secondValue = input + 20;
    }

    [UFunction(FunctionFlags.BlueprintCallable | FunctionFlags.BlueprintEvent)]
    public partial void ReadCallableValue(int input, out int value);

    public partial void ReadCallableValue_Implementation(int input, out int value)
    {
        value = input + 10;
    }

    [UFunction(FunctionFlags.BlueprintEvent)]
    public partial void ReceiveValue(int input);

    public partial void ReceiveValue_Implementation(int input)
    {
    }

    [UFunction(FunctionFlags.BlueprintEvent)]
    public partial void UpdateValue(ref int value);

    public partial void UpdateValue_Implementation(ref int value)
    {
        value += 10;
    }

    [UFunction(FunctionFlags.BlueprintEvent)]
    public partial void UpdateValues(ref int firstValue, ref int secondValue);

    public partial void UpdateValues_Implementation(ref int firstValue, ref int secondValue)
    {
        firstValue += 10;
        secondValue -= 20;
    }

    [UFunction(FunctionFlags.BlueprintCallable | FunctionFlags.BlueprintEvent)]
    public partial int UpdateMixed(int input, ref int value, out int output);

    public partial int UpdateMixed_Implementation(int input, ref int value, out int output)
    {
        value += input;
        output = value * 2;
        return value + output;
    }

    [UFunction(FunctionFlags.BlueprintCallable)]
    public bool VerifyRefParameters()
    {
        int value = 7;
        UpdateValue(ref value);
        if (value != 17)
        {
            return false;
        }

        int firstValue = -10;
        int secondValue = 20;
        UpdateValues(ref firstValue, ref secondValue);
        if (firstValue != 0 || secondValue != 0)
        {
            return false;
        }

        int input = 3;
        value = 4;
        int result = UpdateMixed(input, ref value, out int output);
        return input == 3 && value == 7 && output == 14 && result == 21;
    }

    [UFunction(FunctionFlags.BlueprintEvent)]
    public partial void CopyString(string input, out string output);

    public partial void CopyString_Implementation(string input, out string output)
    {
        output = input;
    }

    [UFunction(FunctionFlags.BlueprintCallable)]
    public bool VerifyStringParameters()
    {
        foreach (string input in new[] { string.Empty, "ref/out: Unicode \u4e2d\u6587", new string('x', 8192) })
        {
            CopyString(input, out string output);
            if (output != input)
            {
                return false;
            }
        }

        return true;
    }

    // Invoke this override from native code with a populated spec. The const
    // input must remain unchanged when the managed callback returns (#678).
    public override float CalculateBaseMagnitude(FGameplayEffectSpec spec)
    {
        return GetSetByCallerMagnitudeByName(spec, new FName("ConstReferenceInput"));
    }

    // Native ProcessEvent callers must retain a valid attribute as well as
    // receive the changed magnitude when this callback returns.
    [UFunction(FunctionFlags.BlueprintEvent)]
    public partial void UpdateEvaluated(ref FGameplayModifierEvaluatedData evaluatedData);

    public partial void UpdateEvaluated_Implementation(ref FGameplayModifierEvaluatedData evaluatedData)
    {
        evaluatedData.Magnitude += 10;
    }

    [UFunction(FunctionFlags.BlueprintEvent)]
    public partial UObject ReadObject(UObject input, out UObject output);

    public partial UObject ReadObject_Implementation(UObject input, out UObject output)
    {
        output = input;
        return input;
    }

    [UFunction(FunctionFlags.BlueprintEvent)]
    public partial void ExchangeObjects(ref UObject first, ref UObject second);

    public partial void ExchangeObjects_Implementation(ref UObject first, ref UObject second)
    {
        (first, second) = (second, first);
    }

    [UFunction(FunctionFlags.BlueprintEvent)]
    public partial void ReplaceObject(UObject input, ref UObject value, out UObject previous);

    public partial void ReplaceObject_Implementation(UObject input, ref UObject value, out UObject previous)
    {
        previous = value;
        value = input;
    }

    [UFunction(FunctionFlags.BlueprintCallable)]
    public bool VerifyObjectParameters()
    {
        UObject first = NewObject<USphereComponent>();
        UObject second = NewObject<UBoxComponent>();
        if (ReadObject(first, out UObject output) != first || output != first)
        {
            return false;
        }

        UObject originalFirst = first;
        UObject originalSecond = second;
        ExchangeObjects(ref first, ref second);
        if (first != originalSecond || second != originalFirst)
        {
            return false;
        }

        ReplaceObject(originalFirst, ref first, out UObject previous);
        if (first != originalFirst || previous != originalSecond)
        {
            return false;
        }

        ReplaceObject(null!, ref first, out previous);
        if (first != null || previous != originalFirst)
        {
            return false;
        }

        UObject nullValue = null!;
        ExchangeObjects(ref nullValue, ref second);
        return nullValue == originalFirst && second == null && ReadObject(null!, out output) == null && output == null;
    }
}
