namespace UnrealSharpBuildTool;

public static class TemplateUtilities
{
	public static void WriteTemplateToFile(string templateName, string fileName, string suffix, string outputDirectory, Dictionary<string, string> arguments)
	{
		string templatePath = Path.Combine(Program.GetPluginDirectory(), "Templates", $"{templateName}.template");
		if (!File.Exists(templatePath))
		{
			throw new FileNotFoundException($"Template file '{templatePath}' not found.");
		}

		string templateContent = File.ReadAllText(templatePath);
		string result = templateContent.ReplacePlaceholders(arguments);
		
		if (!Directory.Exists(outputDirectory))
		{
			Directory.CreateDirectory(outputDirectory);
		}
		
		string outputPath = Path.Combine(outputDirectory, fileName + "." + suffix);
		File.WriteAllText(outputPath, result);
	}
	
	public static string ReplacePlaceholders(this string template, Dictionary<string, string> arguments)
	{
		string result = template;
		
		foreach (KeyValuePair<string, string> kvp in arguments)
		{
			result = result.Replace($"{{{kvp.Key}}}", kvp.Value);
		}
		
		return result;
	}
}