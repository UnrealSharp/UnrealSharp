using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;

namespace UnrealSharp.Interop;

public static class ExportedFunctionsManager
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] 
    delegate void NativeFunctionDelegate(IntPtr nativeFunctionPtr);
    
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    unsafe delegate void RegisterFunctionsCallback(IntPtr nativeFunctionPtr, char* nativeFunctionName);
    
    private static readonly Dictionary<string, FieldInfo> UnmanagedDelegates = new();

    public static void Initialize(IntPtr NativeExportFunctionsPtr)
    {
        try
        {
            unsafe
            {
                foreach (Type type in Assembly.GetExecutingAssembly().GetTypes())
                {
                    if (!Attribute.IsDefined(type, typeof(NativeCallbacksAttribute)))
                    {
                        continue;
                    }

                    foreach (FieldInfo field in type.GetFields(BindingFlags.Public | BindingFlags.NonPublic |
                                                               BindingFlags.Static))
                    {
                        if (!field.IsStatic || !field.FieldType.IsUnmanagedFunctionPointer)
                        {
                            continue;
                        }
                        
                        UnmanagedDelegates.TryAdd(type.Name + "." + field.Name, field);
                    }
                }

            
                NativeFunctionDelegate registerFunctions =
                    Marshal.GetDelegateForFunctionPointer<NativeFunctionDelegate>(NativeExportFunctionsPtr);
                IntPtr registerFunctionsCallback =
                    Marshal.GetFunctionPointerForDelegate<RegisterFunctionsCallback>(RegisterFunctions);

                registerFunctions(registerFunctionsCallback);
                
                foreach (KeyValuePair<string, FieldInfo> unmanagedDelegate in UnmanagedDelegates)
                {
                    if (unmanagedDelegate.Value.GetValue(null) == null)
                    {
                        Console.WriteLine($"Failed to initialize {unmanagedDelegate.Key}.");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to initialize native functions: {ex}");
        }
    }

    private static unsafe void RegisterFunctions(IntPtr nativeFunctionPtr, char* nativeFunctionName)
    {
        string nativeFunctionNameString = new string(nativeFunctionName);
        
        try
        {
            if (!UnmanagedDelegates.TryGetValue(nativeFunctionNameString, out FieldInfo unmanagedDelegate))
            {
                throw new Exception($"Failed to find {nativeFunctionNameString} in {nameof(UnmanagedDelegates)}.");
            }
            
            unmanagedDelegate.SetValue(null, nativeFunctionPtr);
        }
        catch (Exception e)
        {
            Console.WriteLine($"Failed to register native function \"{nativeFunctionNameString}\" exception: {e}");
        }
    }
}