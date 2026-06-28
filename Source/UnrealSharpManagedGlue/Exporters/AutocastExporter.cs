using System;
using System.Collections.Generic;
using System.Linq;
using EpicGames.UHT.Types;
using UnrealSharpManagedGlue.PropertyTranslators;
using UnrealSharpManagedGlue.SourceGeneration;
using UnrealSharpManagedGlue.Utilities;

namespace UnrealSharpManagedGlue.Exporters;

file static class AutocastNaming
{
	private static string StripFPrefix(string name) => name.Length > 1 && name[0] == 'F' && char.IsUpper(name[1]) ? name.Substring(1) : name;
	private static string StripConvPrefix(string name) => name.StartsWith("Conv_", StringComparison.OrdinalIgnoreCase) ? name.Substring("Conv_".Length) : name;

	private static string GetReturnShortName(UhtFunction function)
	{
		if (function.ReturnProperty is UhtStructProperty structReturn)
		{
			return StripFPrefix(structReturn.ScriptStruct.GetStructName());
		}

		string managed = function.ReturnProperty!.GetTranslator()!.GetManagedType(function.ReturnProperty!);
		return managed.Split('.').Last();
	}

	private static bool TryStripPrefix(ref string name, string prefix)
	{
		if (!name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}

		name = name.Substring(prefix.Length);
		return true;
	}

	private static bool TryStripSourceToToken(ref string name, string source)
	{
		string token = source + "To";
		if (!name.StartsWith(token, StringComparison.OrdinalIgnoreCase) || name.Length <= token.Length)
		{
			return false;
		}

		name = name.Substring(token.Length);
		return true;
	}

	public static string GetCleanName(UhtStruct owner, UhtFunction function)
	{
		string originalFunctionName = function.GetFunctionName();
		string structName = owner.GetStructName();
		string structShortName = StripFPrefix(structName);
		bool returningStruct = function.ReturnProperty is UhtStructProperty;
		string returnShortName = GetReturnShortName(function);

		string functionName = originalFunctionName;
		bool mechanical = false;

		if (functionName.StartsWith("Conv_", StringComparison.OrdinalIgnoreCase))
		{
			functionName = functionName.Substring("Conv_".Length);
			mechanical = true;
		}

		mechanical |= TryStripPrefix(ref functionName, structName + "_") || TryStripPrefix(ref functionName, structShortName + "_");
		mechanical |= TryStripSourceToToken(ref functionName, structShortName) || TryStripSourceToToken(ref functionName, structName);

		if (!mechanical && (originalFunctionName == returnShortName || originalFunctionName == "F" + returnShortName))
		{
			mechanical = true;
		}

		if (returningStruct && mechanical)
		{
			return "To" + returnShortName;
		}

		return functionName.StartsWith("To", StringComparison.OrdinalIgnoreCase) ? functionName : "To" + StripFPrefix(functionName);
	}

	public static Dictionary<UhtFunction, string> ResolveCollisions(UhtStruct owner, IReadOnlyList<UhtFunction> functions)
	{
		Dictionary<UhtFunction, string> functionToResultName = new Dictionary<UhtFunction, string>();
		Dictionary<string, int> nameCounts = new Dictionary<string, int>();

		foreach (UhtFunction function in functions)
		{
			string name = GetCleanName(owner, function);
			functionToResultName[function] = name;
			nameCounts[name] = nameCounts.TryGetValue(name, out int count) ? count + 1 : 1;
		}

		foreach (UhtFunction function in functions)
		{
			if (nameCounts[functionToResultName[function]] > 1)
			{
				functionToResultName[function] = StripFPrefix(StripConvPrefix(function.GetFunctionName()));
			}
		}

		return functionToResultName;
	}
}

public static class AutocastExporter
{
	public static void ExportAutocast(GeneratorStringBuilder stringBuilder, List<UhtFunction> unorderedAutocastFunctions)
	{
		Dictionary<UhtStruct, List<UhtFunction>> conversionStructs = new();

		foreach (UhtFunction function in unorderedAutocastFunctions)
		{
			UhtStructProperty structProperty = (UhtStructProperty)function.Properties.First();
			UhtStruct conversionStruct = structProperty.ScriptStruct;

			if (!conversionStructs.ContainsKey(conversionStruct))
			{
				conversionStructs[conversionStruct] = new List<UhtFunction>();
			}

			conversionStructs[conversionStruct].Add(function);
		}

		foreach ((UhtStruct conversionStruct, List<UhtFunction> functions) in conversionStructs)
		{
			List<UhtFunction> exported = functions
				.Where(function => !ReturnValueIsSameAsParameter(function))
				.ToList();

			if (exported.Count == 0)
			{
				continue;
			}

			Dictionary<UhtFunction, string> names = AutocastNaming.ResolveCollisions(conversionStruct, exported);

			stringBuilder.AppendLine();
			stringBuilder.AppendLine($"namespace {conversionStruct.GetNamespace()}");
			stringBuilder.OpenBrace();

			stringBuilder.DeclareType(conversionStruct, "record struct", conversionStruct.GetStructName());

			foreach (UhtFunction function in exported)
			{
				string methodName = names[function];
				string returnType = function.ReturnProperty!.GetTranslator()!.GetManagedType(function.ReturnProperty!);
				string functionCall = $"{function.Outer!.GetFullManagedName()}.{function.GetFunctionName()}";

				bool isToString = methodName == "ToString";
				string modifiers = isToString ? "override " : "";
				string memberSuffix = isToString ? "()" : "";

				stringBuilder.AppendLine($"public {modifiers}{returnType} {methodName}{memberSuffix} => {functionCall}(this);");
			}

			stringBuilder.CloseBrace();
			stringBuilder.CloseBrace();
		}
	}

	static bool ReturnValueIsSameAsParameter(UhtFunction function)
	{
		UhtProperty returnProperty = function.ReturnProperty!;
		foreach (UhtType uhtType in function.Children)
		{
			UhtProperty parameter = (UhtProperty)uhtType;

			if (parameter != returnProperty && parameter.IsSameType(returnProperty))
			{
				return true;
			}
		}

		return false;
	}
}