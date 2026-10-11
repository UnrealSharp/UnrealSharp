using UnrealSharp.Attributes;
using UnrealSharp.CoreUObject;

namespace TestSourceGen;

[UClass]
public partial class UOutParametersTest : UObject
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
}
