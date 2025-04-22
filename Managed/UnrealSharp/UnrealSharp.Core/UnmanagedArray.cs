using System.Runtime.InteropServices;
using UnrealSharp.Core.Interop;
using UnrealSharp.Core.Marshallers;

namespace UnrealSharp.Core;

[StructLayout(LayoutKind.Sequential)]
public struct UnmanagedArray
{
    public IntPtr Data;
    public int ArrayNum;
    public int ArrayMax;
    
    public void Destroy()
    {
        unsafe
        {
            fixed (UnmanagedArray* ptr = &this)
            {
                FScriptArrayExporter.CallDestroy(ptr);
            }
            
            Data = IntPtr.Zero;
            ArrayNum = 0;
            ArrayMax = 0;
        }
    }
    
    public List<T> ToBlittableList<T>() where T : unmanaged
    {
        List<T> list = new List<T>(ArrayNum);
        
        unsafe
        {
            T* data = (T*) Data.ToPointer();
            for (int i = 0; i < ArrayNum; i++)
            {
                list.Add(data[i]);
            }
        }
        
        return list;
    }
    
    public List<T> ToListWithMarshaller<T>(Func<IntPtr, int, T> resolver)
    {
        List<T> list = new List<T>(ArrayNum);

        for (int i = 0; i < ArrayNum; i++)
        {
            list.Add(resolver(Data, i));
        }

        return list;
    }
    
    public void ForEachWithMarshaller<T>(Func<IntPtr, int, T> resolver, Action<T> action)
    {
        for (int i = 0; i < ArrayNum; i++)
        {
            T item = resolver(Data, i);
            action(item);
        }
    }
}