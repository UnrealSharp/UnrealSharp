using System.Collections.Generic;
using System.IO;
using AutomationTool;

namespace UnrealSharp.Automation.Utilities;

public static class TemplateUtilities
{
	public static void WriteTemplateToFile(BuildCommand buildCommand, string templateName, string fileName, string suffix, string outputDirectory, Dictionary<string, string> arguments)
	{
		string RootDirectory = buildCommand.GetUnrealSharpRootFolder(); 
		string TemplatePath = Path.Combine(RootDirectory, "Templates", $"{templateName}.template");
		if (!File.Exists(TemplatePath))
		{
			throw new FileNotFoundException($"Template file '{TemplatePath}' not found.");
		}

		string TemplateContent = File.ReadAllText(TemplatePath);
		string Result = TemplateContent.ReplacePlaceholders(arguments);
		
		if (!Directory.Exists(outputDirectory))
		{
			Directory.CreateDirectory(outputDirectory);
		}
		
		string OutputPath = Path.Combine(outputDirectory, fileName + "." + suffix);
		File.WriteAllText(OutputPath, Result);
	}
	
	public static string ReplacePlaceholders(this string template, Dictionary<string, string> arguments)
	{
		string Result = template;
		
		foreach (KeyValuePair<string, string> Kvp in arguments)
		{
			Result = Result.Replace($"{{{Kvp.Key}}}", Kvp.Value);
		}
		
		return Result;
	}
}