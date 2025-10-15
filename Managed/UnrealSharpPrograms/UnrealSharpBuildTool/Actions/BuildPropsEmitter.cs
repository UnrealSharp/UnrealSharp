using System.Xml;

namespace UnrealSharpBuildTool.Actions;

public class CreateDirectoryBuildPropsFile : BuildToolAction
{
    public override bool RunAction()
    {
        BuildPropsEmitter.GenerateBuildPropsFile(Program.GetScriptFolder());
        return true;
    }
}

public static class BuildPropsEmitter
{
    private const string ReferencesLabel = "UnrealSharpRefs";
    private const string AnalyzersLabelUser = "UnrealSharpAnalyzers_User";
    private const string AnalyzerLabelGlobal = "UnrealSharpAnalyzers_Global";
    private const string Properties = "UnrealSharpProperties";
    
    private const string SolutionDirProperty = "$(SolutionDir)";

    public static void GenerateBuildPropsFile(string directory)
    {
        string generatedBuildPropsPath = Path.Combine(directory, "Directory.Build.props");
        string solutionDir = Path.GetDirectoryName(Program.GetSolutionFile())!;

        XmlDocument csprojDocument = new XmlDocument();

        if (File.Exists(generatedBuildPropsPath))
        {
            csprojDocument.Load(generatedBuildPropsPath);
        }
        else
        {
            csprojDocument.EnsureProjectRoot();
        }

        XmlElement projectRoot = csprojDocument.DocumentElement!;
            
        XmlElement propertyGroup = csprojDocument.FindOrMakeGeneratedLabeledPropertyGroup(projectRoot, Properties);
        EnsureSolutionDirFallback(csprojDocument, propertyGroup);
        AppendProperties(csprojDocument, propertyGroup, solutionDir);
        AppendConstantDefines(csprojDocument, propertyGroup);

        XmlElement refsGroup = csprojDocument.FindOrMakeGeneratedLabeledItemGroup(projectRoot, ReferencesLabel);
        string binariesMsbuildPath = GetRelativePathToBinaries(directory, solutionDir);
        csprojDocument.AppendReference(refsGroup, "UnrealSharp", binariesMsbuildPath);
        csprojDocument.AppendReference(refsGroup, "UnrealSharp.Core", binariesMsbuildPath);
        csprojDocument.AppendReference(refsGroup, "UnrealSharp.Log", binariesMsbuildPath);

        XmlElement analyzersGroup = csprojDocument.FindOrMakeGeneratedLabeledItemGroup(projectRoot, AnalyzersLabelUser);
        analyzersGroup.SetAttribute("Condition", "!$(MSBuildProjectName.EndsWith('.Glue'))");
        AppendSourceGeneratorReference(csprojDocument, analyzersGroup, directory, solutionDir, "UnrealSharp.GlueGenerator.dll");
        AppendSourceGeneratorReference(csprojDocument, analyzersGroup, directory, solutionDir, "UnrealSharp.Analyzers.dll");
        
        XmlElement globalAnalyzersGroup = csprojDocument.FindOrMakeGeneratedLabeledItemGroup(projectRoot, AnalyzerLabelGlobal);
        AppendSourceGeneratorReference(csprojDocument, globalAnalyzersGroup, directory, solutionDir, "UnrealSharp.SourceGenerators.dll");

        csprojDocument.Save(generatedBuildPropsPath);
    }

    static void AddProperty(string name, string value, XmlDocument doc, XmlElement propertyGroup)
    {
        XmlElement element = doc.GetOrCreateChild(propertyGroup, name);
        element.InnerText = value;
    }

    static void EnsureSolutionDirFallback(XmlDocument doc, XmlElement propertyGroup)
    {
        XmlElement fallback = doc.CreateElement("SolutionDir");
        fallback.SetAttribute("Condition", $"'{SolutionDirProperty}' == ''");
        fallback.InnerText = "$(MSBuildThisFileDirectory)";
        propertyGroup.AppendChild(fallback);
    }

    static void AppendProperties(XmlDocument doc, XmlElement propertyGroup, string solutionDir)
    {
        AddProperty("AllowUnsafeBlocks", "true", doc, propertyGroup);
        AddProperty("EnableDynamicLoading", "true", doc, propertyGroup);
        AddProperty("LangVersion", "latest", doc, propertyGroup);
        AddProperty("AllowUnsafeBlocks", "true", doc, propertyGroup);
        
        string absoluteOutput = Program.GetOutputPath(includeVersion: false);
        string relFromSolutionToOutput = GetRelativePath(solutionDir, absoluteOutput);
        string msbuildOutput = JoinMsbuildProperty(SolutionDirProperty, relFromSolutionToOutput);
        
        AddProperty("OutputPath", msbuildOutput, doc, propertyGroup);
    }

    static void AddConstDefine(string value, XmlDocument doc, XmlElement propertyGroup, string? condition = null)
    {
        XmlElement define = doc.CreateElement("DefineConstants");
        define.InnerText = value;

        if (condition != null)
        {
            define.SetAttribute("Condition", condition);
        }

        propertyGroup.AppendChild(define);
    }

    static void AppendConstantDefines(XmlDocument doc, XmlElement propertiesGroup)
    {
        AddConstDefine("WITH_EDITOR", doc, propertiesGroup);
        AddConstDefine("$(DefineConstants.Replace('WITH_EDITOR;', '').Replace('WITH_EDITOR', ''))", doc, propertiesGroup, "'$(DisableWithEditor)' == 'true'");
        AddConstDefine("$(DefineConstants);$(DefineAdditionalConstants)", doc, propertiesGroup, "'$(DefineAdditionalConstants)' != ''");
    }

    static string GetRelativePathToBinaries(string projectRoot, string solutionDir)
    {
        string pluginPath = Path.Combine(projectRoot, Program.BuildToolOptions.PluginDirectory);
        string binariesPath = Path.Combine(pluginPath, "Binaries", "Managed");
        string rel = GetRelativePath(solutionDir, binariesPath);
        return JoinMsbuildProperty(SolutionDirProperty, rel);
    }

    static XmlElement AppendSourceGeneratorReference(XmlDocument doc, XmlElement itemGroup, string projectRoot, string solutionDir, string generatorName)
    {
        string relativePath = GetRelativePathToBinaries(projectRoot, solutionDir);
        relativePath = Path.Combine(relativePath, Program.GetNetStandardVersion(), generatorName);
        XmlElement analyzer = doc.AppendAnalyzer(itemGroup, relativePath);
        analyzer.SetAttribute("Private", "false");
        analyzer.SetAttribute("CopyToPublishDirectory", "Never");
        return analyzer;
    }

    static string JoinMsbuildProperty(string property, string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return property;
        }

        if (path.StartsWith("$("))
        {
            return $"{property}{path}";
        }

        return $"{property}\\{path}";
    }

    public static string GetRelativePath(string basePath, string targetPath)
    {
        Uri baseUri = new Uri(basePath.EndsWith(Path.DirectorySeparatorChar.ToString()) ? basePath : basePath + Path.DirectorySeparatorChar);
        Uri targetUri = new Uri(targetPath);
        Uri relativeUri = baseUri.MakeRelativeUri(targetUri);
        string s = Uri.UnescapeDataString(relativeUri.ToString());
        return OperatingSystem.IsWindows() ? s.Replace('/', '\\') : s;
    }
}
