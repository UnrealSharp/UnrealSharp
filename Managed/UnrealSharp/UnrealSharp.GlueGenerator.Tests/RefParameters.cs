using UnrealSharp.Attributes;
using UnrealSharp.CoreUObject;

namespace TestSourceGen;

[USingleDelegate]
public delegate void FRefParameterDelegate(ref int value);

[UClass]
public partial class URefParametersTest : UObject
{
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
}
