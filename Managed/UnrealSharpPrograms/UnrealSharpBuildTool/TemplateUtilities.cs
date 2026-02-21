namespace UnrealSharpBuildTool;

public static class TemplateUtilities
{
	public static void WriteTemplateToFile(string templateName, string fileName, string suffix, string outputDirectory, object[] arguments)
	{
		string templatePath = Path.Combine(Program.GetPluginDirectory(), "Templates", $"{templateName}.template");
		if (!File.Exists(templatePath))
		{
			throw new FileNotFoundException($"Template file '{templatePath}' not found.");
		}

		string templateContent = File.ReadAllText(templatePath);
		string result = string.Format(templateContent, arguments);
		
		if (!Directory.Exists(outputDirectory))
		{
			Directory.CreateDirectory(outputDirectory);
		}
		
		string outputPath = Path.Combine(outputDirectory, fileName + "." + suffix);
		File.WriteAllText(outputPath, result);
	}
}