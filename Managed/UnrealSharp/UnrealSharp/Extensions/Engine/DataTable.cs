using UnrealSharp.Attributes;
using UnrealSharp.Interop;
using UnrealSharp.UnrealSharpCore;

namespace UnrealSharp.Engine;

public partial class UDataTable
{
    #if !PACKAGE
    /// <summary>
    /// Gets the table as a CSV string.
    /// </summary>
    public string ToCSV => UCSDataTableExtensions.GetTableAsCSV(this);
    
    /// <summary>
    /// Gets the table as a JSON string.
    /// </summary>
    public string ToJSON => UCSDataTableExtensions.GetTableAsJSON(this);
    #endif
    
    /// <summary>
    /// Gets the number of rows in the table.
    /// </summary>
    public int NumRows => GetNumRows();
    
    /// <summary>
    /// Gets the row names of the table.
    /// </summary>
    public IList<FName> RowNames => GetRowNames();

    /// <summary>
    /// Find a row in the table by name. Assumes the row is valid.
    /// </summary>
    /// <param name="rowName">The name of the row to find</param>
    /// <typeparam name="T">The type of the row to find</typeparam>
    /// <returns>The row if found, otherwise the default value of the type</returns>
    public T FindRow<T>(FName rowName) where T : struct
    {
        Type type = typeof(T);
        
        if (type.GetCustomAttributes(typeof(UStructAttribute), false).Length == 0)
        {
            throw new Exception($"The type {type.Name} must be a UStruct.");
        }
        
        IntPtr rowPtr = UDataTableExporter.CallGetRow(NativeObject, rowName);
        return (T)Activator.CreateInstance(type, rowPtr)!;
    }
    
    /// <summary>
    /// Try to find a row in the table by name. Checks if the row is valid before returning it.
    /// </summary>
    /// <param name="rowName">The name of the row to find</param>
    /// <param name="value">The row if found, otherwise null</param>
    /// <typeparam name="T">The type of the row to find</typeparam>
    /// <returns>True if the row was found, otherwise false</returns>
    public bool TryFindRow<T>(FName rowName, out T? value) where T : struct
    {
        value = null;
        Type type = typeof(T);
        
        if (type.GetCustomAttributes(typeof(UStructAttribute), false).Length == 0)
        {
            throw new Exception($"The type {type.Name} must be a UStruct.");
        }
        
        IntPtr rowPtr = UDataTableExporter.CallGetRow(NativeObject, rowName);
        if (rowPtr == IntPtr.Zero)
        {
            return false;
        }
        
        value = (T)Activator.CreateInstance(type, rowPtr)!;
        return true;
    }
    
    /// <summary>
    /// Check if a row exists in the table by name.
    /// </summary>
    /// <param name="rowName">The name of the row to check</param>
    /// <returns>True if the row exists, otherwise false</returns>
    public bool HasRow(FName rowName)
    {
        return UDataTableFunctionLibrary.DoesRowExist(this, rowName);
    }
    
    /// <summary>
    /// Get the row names of the table.
    /// </summary>
    /// <returns>The row names of the table</returns>
    public void ForEachRow<T>(Action<FName, T> action) where T : struct
    {
        IList<FName> rowNames = GetRowNames();
        foreach (FName rowName in rowNames)
        {
            action(rowName, FindRow<T>(rowName));
        }
    }
    
    /// <summary>
    /// Get the row names of the table.
    /// </summary>
    /// <param name="action">The action to perform on each row</param>
    /// <typeparam name="T">The type of the row</typeparam>
    public void ForEachRow<T>(Action<T> action) where T : struct
    {
        IList<FName> rowNames = GetRowNames();
        foreach (FName rowName in rowNames)
        {
            action(FindRow<T>(rowName));
        }
    }
    
    /// <summary>
    /// Get the row names of the table.
    /// </summary>
    /// <param name="action">The action to perform on each row</param>
    public void ForEachRow(Action<FName> action)
    {
        IList<FName> rowNames = GetRowNames();
        foreach (FName rowName in rowNames)
        {
            action(rowName);
        }
    }
    
    private IList<FName> GetRowNames()
    {
        UDataTableFunctionLibrary.GetRowNames(this, out IList<FName> outRowNames);
        return outRowNames;
    }
    
    private int GetNumRows()
    {
        return GetRowNames().Count;
    }
    
    public static bool IsUStruct<T>() where T : struct
    {
        return typeof(T).GetCustomAttributes(typeof(UStructAttribute), false).Length > 0;
    }
}