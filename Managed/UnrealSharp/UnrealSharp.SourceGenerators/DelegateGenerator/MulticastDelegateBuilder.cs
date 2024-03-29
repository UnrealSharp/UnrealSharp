using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;

namespace UnrealSharp.SourceGenerators;

public class MulticastDelegateBuilder : DelegateBuilder
{
    public override void StartBuilding(StringBuilder stringBuilder, INamedTypeSymbol delegateSymbol, INamedTypeSymbol classSymbol)
    {
        GenerateAddFunction(stringBuilder, delegateSymbol);
        GenerateAddOperator(stringBuilder, delegateSymbol, classSymbol.Name);
            
        GenerateGetInvoker(stringBuilder, delegateSymbol);
            
        GenerateRemoveOperator(stringBuilder, delegateSymbol, classSymbol.Name);
        GenerateRemoveFunction(stringBuilder, delegateSymbol);
        
        GenerateContainsFunction(stringBuilder, delegateSymbol);
        
        //Check if the class has an Invoker method already
        if (!classSymbol.GetMembers("Invoker").Any())
        {
            GenerateInvoke(stringBuilder, delegateSymbol);
        }
    }
    
    void GenerateAddFunction(StringBuilder stringBuilder, INamedTypeSymbol delegateSymbol)
    {
        var delegateInvokeMethod = delegateSymbol.DelegateInvokeMethod;

        if (delegateInvokeMethod == null)
        {
            return;
        }
        
        if (delegateInvokeMethod.Parameters.IsEmpty)
        {
            stringBuilder.AppendLine("    public void Add(Action action)");
        }
        else
        {
            stringBuilder.Append("    public void Add(Action<");
            stringBuilder.Append(string.Join(", ", delegateInvokeMethod.Parameters.Select(x => $"{x.Type}")));
            stringBuilder.Append("> action)");
            stringBuilder.AppendLine();
        }
        
        stringBuilder.AppendLine("    {");
        stringBuilder.AppendLine("        try");
        stringBuilder.AppendLine("        {");
        CastToUnrealSharpObject(stringBuilder);
        stringBuilder.AppendLine($"            FMulticastDelegatePropertyExporter.CallAddDelegate(NativeProperty, NativeDelegate, unrealSharpObject.NativeObject, action.Method.Name);");
        stringBuilder.AppendLine("        }");
        stringBuilder.AppendLine("        catch (Exception ex)");
        stringBuilder.AppendLine("        {");
        stringBuilder.AppendLine("            System.Console.WriteLine(ex);");
        stringBuilder.AppendLine("        }");
        stringBuilder.AppendLine("    }");
        stringBuilder.AppendLine();
    }
    
    void GenerateAddOperator(StringBuilder stringBuilder, INamedTypeSymbol delegateSymbol, string className)
    {
        var delegateInvokeMethod = delegateSymbol.DelegateInvokeMethod;

        if (delegateInvokeMethod == null)
        {
            return;
        }
        
        if (delegateInvokeMethod.Parameters.IsEmpty)
        {
            stringBuilder.AppendLine($"    public static {className} operator +({className} thisDelegate, Action action)");
        }
        else
        {
            stringBuilder.Append($"    public static {className} operator +({className} thisDelegate, Action<");
            stringBuilder.Append(string.Join(", ", delegateInvokeMethod.Parameters.Select(x => $"{x.Type}")));
            stringBuilder.Append("> action)");
            stringBuilder.AppendLine();
        }
        
        stringBuilder.AppendLine("    {");
        stringBuilder.AppendLine("        try");
        stringBuilder.AppendLine("        {");
        CastToUnrealSharpObject(stringBuilder, "thisDelegate");
        stringBuilder.AppendLine($"            thisDelegate.Add(action);");
        stringBuilder.AppendLine("        }");
        stringBuilder.AppendLine("        catch (Exception ex)");
        stringBuilder.AppendLine("        {");
        stringBuilder.AppendLine("            System.Console.WriteLine(ex);");
        stringBuilder.AppendLine("        }");
        stringBuilder.AppendLine("        return thisDelegate;");
        stringBuilder.AppendLine("    }");
        stringBuilder.AppendLine();
    }
    
    void GenerateRemoveOperator(StringBuilder stringBuilder, INamedTypeSymbol delegateSymbol, string className)
    {
        var delegateInvokeMethod = delegateSymbol.DelegateInvokeMethod;

        if (delegateInvokeMethod == null)
        {
            return;
        }
        
        if (delegateInvokeMethod.Parameters.IsEmpty)
        {
            stringBuilder.AppendLine($"    public static {className} operator -({className} thisDelegate, Action action)");
        }
        else
        {
            stringBuilder.Append($"    public static {className} operator -({className} thisDelegate, Action<");
            stringBuilder.Append(string.Join(", ", delegateInvokeMethod.Parameters.Select(x => $"{x.Type}")));
            stringBuilder.Append("> action)");
            stringBuilder.AppendLine();
        }
        
        stringBuilder.AppendLine("    {");
        stringBuilder.AppendLine("        try");
        stringBuilder.AppendLine("        {");
        CastToUnrealSharpObject(stringBuilder, "thisDelegate");
        stringBuilder.AppendLine($"            thisDelegate.Remove(action);");
        stringBuilder.AppendLine("        }");
        stringBuilder.AppendLine("        catch (Exception ex)");
        stringBuilder.AppendLine("        {");
        stringBuilder.AppendLine("            System.Console.WriteLine(ex);");
        stringBuilder.AppendLine("        }");
        stringBuilder.AppendLine("        return thisDelegate;");
        stringBuilder.AppendLine("    }");
        stringBuilder.AppendLine();
    }

    void GenerateContainsFunction(StringBuilder stringBuilder, INamedTypeSymbol delegateSymbol)
    {
        var delegateInvokeMethod = delegateSymbol.DelegateInvokeMethod;

        if (delegateInvokeMethod == null)
        {
            return;
        }
        
        if (delegateInvokeMethod.Parameters.IsEmpty)
        {
            stringBuilder.AppendLine("    public bool Contains(Action action)");
        }
        else
        {
            stringBuilder.Append("    public bool Contains(Action<");
            stringBuilder.Append(string.Join(", ", delegateInvokeMethod.Parameters.Select(x => $"{x.Type}")));
            stringBuilder.Append("> action)");
            stringBuilder.AppendLine();
        }
        
        stringBuilder.AppendLine("    {");
        stringBuilder.AppendLine("        try");
        stringBuilder.AppendLine("        {");
        CastToUnrealSharpObject(stringBuilder, "false");
        stringBuilder.AppendLine($"            return FMulticastDelegatePropertyExporter.CallContainsDelegate(NativeProperty, NativeDelegate, unrealSharpObject.NativeObject, action.Method.Name).ToManagedBool();");
        stringBuilder.AppendLine("        }");
        stringBuilder.AppendLine("        catch (Exception ex)");
        stringBuilder.AppendLine("        {");
        stringBuilder.AppendLine("            System.Console.WriteLine(ex);");
        stringBuilder.AppendLine("        }");
        stringBuilder.AppendLine("        return false;");
        stringBuilder.AppendLine("    }");
        stringBuilder.AppendLine();
        
    }

    void GenerateRemoveFunction(StringBuilder stringBuilder, INamedTypeSymbol delegateSymbol)
    {
        var delegateInvokeMethod = delegateSymbol.DelegateInvokeMethod;

        if (delegateInvokeMethod == null)
        {
            return;
        }
        
        if (delegateInvokeMethod.Parameters.IsEmpty)
        {
            stringBuilder.AppendLine($"    public void Remove(Action action)");
        }
        else
        {
            stringBuilder.Append("    public void Remove(Action<");
            stringBuilder.Append(string.Join(", ", delegateInvokeMethod.Parameters.Select(x => $"{x.Type}")));
            stringBuilder.Append("> action)");
            stringBuilder.AppendLine();
        }
        
        stringBuilder.AppendLine("    {");
        stringBuilder.AppendLine("        try");
        stringBuilder.AppendLine("        {");
        CastToUnrealSharpObject(stringBuilder);
        stringBuilder.AppendLine($"            FMulticastDelegatePropertyExporter.CallRemoveDelegate(NativeProperty, NativeDelegate, unrealSharpObject.NativeObject, action.Method.Name);");
        stringBuilder.AppendLine("        }");
        stringBuilder.AppendLine("        catch (Exception ex)");
        stringBuilder.AppendLine("        {");
        stringBuilder.AppendLine("            System.Console.WriteLine(ex);");
        stringBuilder.AppendLine("        }");
        stringBuilder.AppendLine("    }");
        stringBuilder.AppendLine();
    }

    public void CastToUnrealSharpObject(StringBuilder stringBuilder, string returntype = "")
    {
        stringBuilder.AppendLine("            UnrealSharpObject unrealSharpObject = (UnrealSharpObject) action.Target;");
        stringBuilder.AppendLine("            if (unrealSharpObject == null)");
        stringBuilder.AppendLine("            {");
        stringBuilder.AppendLine($"                return {returntype};");
        stringBuilder.AppendLine("            }");
    }
}