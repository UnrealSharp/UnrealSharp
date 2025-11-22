using System.Collections.Immutable;
using System.Diagnostics;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Text;
using UnrealSharp.Core;
using UnrealSharp.Core.Marshallers;
using UnrealSharpBuildTool.Actions;

namespace UnrealSharp.Editor;

public static class IncrementalCompilationManager
{
    private static readonly HashSet<ProjectId> DirtyProjectIds = new();
    
    public static unsafe void GetDependentProjects(string projectName, UnmanagedArray* dependentsArray)
    {
        List<string> dependents = new List<string>();

        Project? foundProject = null;
        foreach (Project project in SolutionManager.UnrealSharpWorkspace.CurrentSolution.Projects)
        {
            if (project.Name != projectName)
            {
                continue;
            }

            foundProject = project;
            break;
        }

        if (foundProject is null)
        {
            return;
        }

        foreach (Project project in SolutionManager.UnrealSharpWorkspace.CurrentSolution.Projects)
        {
            if (project.Id == foundProject.Id)
            {
                continue;
            }

            foreach (ProjectReference projectReference in project.ProjectReferences)
            {
                if (projectReference.ProjectId != foundProject.Id)
                {
                    continue;
                }

                dependents.Add(project.Name);
                break;
            }
        }

        if (dependents.Count == 0)
        {
            return;
        }

        dependentsArray->ToNativeWithMarshaller<string>(StringMarshaller.ToNative, dependents, sizeof(UnmanagedArray));
    }
    
    public static void RemoveSourceFile(string projectName, string filepath)
    {
        Project? foundProject = SolutionManager.GetProjectByName(projectName);

        if (foundProject is null)
        {
            throw new Exception($"Project '{projectName}' not found in solution.");
        }
        
        GenState? state = SolutionManager.GetProjectState(foundProject.Id);
        if (state == null)
        {
            throw new Exception($"Project '{projectName}' not initialized for incremental generation.");
        }

        string fullPath = Path.GetFullPath(filepath);

        if (!state.TreesByPath!.TryGetValue(fullPath, out SyntaxTree? existingTree))
        {
            return;
        }
        
        state.InitialCompilation = state.InitialCompilation!.RemoveSyntaxTrees(existingTree);
        state.TreesByPath.Remove(fullPath);
        
        DirtyProjectIds.Add(foundProject.Id);
    }

    public static void RecompileChangedFile(string projectName, string filepath)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        Project? foundProject = SolutionManager.GetProjectByName(projectName);

        if (foundProject is null)
        {
            throw new Exception($"Project '{projectName}' not found in solution.");
        }

        GenState? state = SolutionManager.GetProjectState(foundProject.Id);
        if (state == null)
        {
            throw new Exception($"Project '{projectName}' not initialized for incremental generation.");
        }

        string fullPath = Path.GetFullPath(filepath);
        string fileContent = File.ReadAllText(fullPath);
        SourceText newText = SourceText.From(fileContent, Encoding.UTF8);

        CSharpParseOptions parseOptions = (CSharpParseOptions)foundProject.ParseOptions!;
        SyntaxTree newTree = CSharpSyntaxTree.ParseText(newText, parseOptions, path: fullPath);

        if (newTree.GetDiagnostics().Any())
        {
            StringBuilder builder = new StringBuilder();

            foreach (Diagnostic diagnostic in newTree.GetDiagnostics())
            {
                if (diagnostic.Severity != DiagnosticSeverity.Error)
                {
                    continue;
                }

                builder.AppendLine(diagnostic.ToString());
            }

            if (builder.Length > 0)
            {
                throw new Exception(builder.ToString());
            }
        }

        if (state.TreesByPath!.TryGetValue(fullPath, out SyntaxTree? existingTree))
        {
            state.InitialCompilation = state.InitialCompilation!.ReplaceSyntaxTree(existingTree, newTree);
        }
        else
        {
            state.InitialCompilation = state.InitialCompilation!.AddSyntaxTrees(newTree);
        }
        
        SyntaxUtilities.ProcessForChangesInUTypes(newTree, existingTree, foundProject);
        
        state.TreesByPath[fullPath] = newTree;
        DirtyProjectIds.Add(foundProject.Id);
        
        stopwatch.Stop();
        LogUnrealSharpEditor.Log($"Processed dirty file '{Path.GetFileName(filepath)}' in project '{projectName}' in {stopwatch.Elapsed.TotalSeconds:F2} seconds.");
    }

    public static void RecompileDirtyProjects()
    {
        foreach (ProjectId dirtyProjectId in DirtyProjectIds)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            
            Project? project = SolutionManager.UnrealSharpWorkspace.CurrentSolution.GetProject(dirtyProjectId);
            if (project is null)
            {
                throw new Exception($"Project with ID '{dirtyProjectId}' not found in solution.");
            }

            GenState? state = SolutionManager.GetProjectState(dirtyProjectId);
            
            if (state is null)
            {
                throw new Exception($"Project '{project.Name}' not initialized for incremental generation.");
            }
            
            CSharpParseOptions parseOptions = (CSharpParseOptions)project.ParseOptions!;
            AnalyzerConfigOptionsProvider analyzerOptions = project.AnalyzerOptions.AnalyzerConfigOptionsProvider;
            
            bool firstTime = state.Driver == null;
            bool analyzersChanged = state.AnalyzerRefCount != project.AnalyzerReferences.Count;
            bool additionalChanged = state.AdditionalDocCount != project.AdditionalDocuments.Count();
            bool optionsChanged = !ReferenceEquals(state.ParseOptions, parseOptions) ||
                                  !ReferenceEquals(state.AnalyzerOptions, analyzerOptions); 
            bool refsChanged = state.MetadataRefCount != project.MetadataReferences.Count;

            if (firstTime || analyzersChanged)
            {
                state.AnalyzerRefCount = project.AnalyzerReferences.Count;
            }

            if (firstTime || state.AdditionalTexts.IsDefault || additionalChanged)
            {
                List<AdditionalText> additionalTexts = new List<AdditionalText>();
                foreach (TextDocument textDocument in project.AdditionalDocuments)
                {
                    Document document = (Document)textDocument;
                    
                    if (document.FilePath is not null)
                    {
                        additionalTexts.Add(new FileAdditionalText(document.FilePath));
                    }
                }

                state.AdditionalTexts = ImmutableArray.CreateRange(additionalTexts);
                state.AdditionalDocCount = project.AdditionalDocuments.Count();
            }

            if (optionsChanged && state.Driver is not null)
            {
                state.Driver = state.Driver
                    .WithUpdatedParseOptions(parseOptions)
                    .WithUpdatedAnalyzerConfigOptions(analyzerOptions);
            }

            if (refsChanged)
            {
                state.MetadataRefCount = project.MetadataReferences.Count;
            }

            GeneratorDriver driver = state.Driver!.RunGeneratorsAndUpdateCompilation(
                state.InitialCompilation!,
                out Compilation updatedCompilation,
                out ImmutableArray<Diagnostic> genDiagnosticsInner);

            state.Driver = driver;

            if (genDiagnosticsInner.Any())
            {
                StringBuilder stringBuilder = new StringBuilder();
                foreach (Diagnostic diagnostic in genDiagnosticsInner)
                {
                    if (diagnostic.Severity != DiagnosticSeverity.Error)
                    {
                        continue;
                    }

                    stringBuilder.AppendLine(diagnostic.ToString());
                }

                if (stringBuilder.Length > 0) throw new Exception("Source generator failed:\n" + stringBuilder);
            }

            state.ParseOptions = parseOptions;
            state.AnalyzerOptions = analyzerOptions;

            stopwatch.Stop();
            LogUnrealSharpEditor.Log($"Project '{project.Name}' generated source in {stopwatch.Elapsed.TotalSeconds:F2} seconds.");
            EmitResultsToDisk(project, updatedCompilation);
        }

        DirtyProjectIds.Clear();

        List<string> assemblies = new List<string>(SolutionManager.UnrealSharpWorkspace.CurrentSolution.Projects.Count());

        foreach (Project project in SolutionManager.UnrealSharpWorkspace.CurrentSolution.Projects)
        {
            assemblies.Add(GetAssemblyOutputPath(project));
        }

        string outputDir = Path.GetDirectoryName(assemblies[0])!;
        AssemblyLoadOrder.EmitLoadOrder(assemblies, outputDir);
    }

    private static string GetOutputPath(Project project, string extension)
    {
        string projectDir = Path.GetDirectoryName(project.FilePath!)!;
        string outputDirectory = project.OutputFilePath != null
            ? Path.GetDirectoryName(project.OutputFilePath)!
            : Path.Combine(projectDir, "bin", "Debug");

        Directory.CreateDirectory(outputDirectory);
        return Path.Combine(outputDirectory, project.AssemblyName + extension);
    }

    private static string GetAssemblyOutputPath(Project project)
    {
        string extension = project.OutputFilePath != null ? Path.GetExtension(project.OutputFilePath) : ".dll";
        return GetOutputPath(project, extension);
    }
    
    private static string GetDebugSymbolExtension()
    {
        if (OperatingSystem.IsWindows())
        {
            return ".pdb";
        }

        if (OperatingSystem.IsMacOS())
        {
            return ".dSYM";
        }
        
        return ".so.debug";
    }

    private static void EmitResultsToDisk(Project project, Compilation updatedCompilation)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();

        string assemblyPath = GetAssemblyOutputPath(project);
        string symbolsPath = GetOutputPath(project, GetDebugSymbolExtension());

        EmitOptions emitOptions = new EmitOptions(debugInformationFormat: DebugInformationFormat.PortablePdb);
        FileStream assemblyStream = File.Create(assemblyPath);
        FileStream symbolsStream = File.Create(symbolsPath);

        EmitResult emitResult = updatedCompilation.Emit(assemblyStream, symbolsStream, options: emitOptions);

        assemblyStream.Close();
        symbolsStream.Close();

        if (!emitResult.Success)
        {
            StringBuilder stringBuilder = new StringBuilder(256);

            foreach (Diagnostic diagnostic in emitResult.Diagnostics)
            {
                if (diagnostic.Severity != DiagnosticSeverity.Error)
                {
                    continue;
                }

                if (stringBuilder.Length > 0)
                {
                    stringBuilder.Append(Environment.NewLine);
                }

                stringBuilder.Append(diagnostic);
            }

            throw new InvalidOperationException(stringBuilder.ToString());
        }

        stopwatch.Stop();
        LogUnrealSharpEditor.Log($"Project '{project.Name}' emitted in {stopwatch.Elapsed.TotalSeconds:F2} seconds.");
    }
}