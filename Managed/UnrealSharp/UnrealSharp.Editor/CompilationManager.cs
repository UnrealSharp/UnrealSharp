using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.CodeAnalysis.Text;
using UnrealSharp.Core;
using UnrealSharp.Core.Marshallers;
using UnrealSharpBuildTool.Actions;

namespace UnrealSharp.Editor;

file sealed class FileAdditionalText : AdditionalText
{
    public FileAdditionalText(string path)
    {
        Path = path;
    }

    public override string Path { get; }

    public override SourceText? GetText(CancellationToken cancellationToken = default)
    {
        return SourceText.From(File.ReadAllText(Path), Encoding.UTF8);
    }
}

public static class CompilationManager
{
    private static readonly MSBuildWorkspace UnrealSharpWorkspace = MSBuildWorkspace.Create();

    private sealed class GenState
    {
        public GeneratorDriver? Driver;
        public List<ISourceGenerator>? Generators;
        public ImmutableArray<AdditionalText> AdditionalTexts;
        public CSharpParseOptions ParseOptions = null!;
        public AnalyzerConfigOptionsProvider AnalyzerOptions = null!;
        public int AnalyzerRefCount;
        public int AdditionalDocCount;

        public Compilation? InitialCompilation;
        public Dictionary<string, SyntaxTree>? TreesByPath;

        public int MetadataRefCount;
    }

    private static readonly ConcurrentDictionary<ProjectId, GenState> States = new();
    private static readonly HashSet<ProjectId> DirtyProjectIds = new();

    public static async void LoadSolutionAsync(string solutionPath, IntPtr callbackPtr)
    {
        try
        {
            string fullSolutionPath = Path.GetFullPath(solutionPath);
            await UnrealSharpWorkspace.OpenSolutionAsync(fullSolutionPath);
        
            foreach (Project project in UnrealSharpWorkspace.CurrentSolution.Projects)
            {
                await ProcessProject(project);
            }

            unsafe
            {
                delegate* unmanaged[Cdecl]<void> callback = (delegate* unmanaged[Cdecl]<void>)callbackPtr;
                callback();
            }
        }
        catch (Exception exception)
        {
            LogUnrealSharpEditor.LogError($"Failed to load solution '{solutionPath}': {exception}");
        }
    }

    public static async Task ProcessProject(Project project)
    {
        try
        {
            GenState state = new GenState();
            States[project.Id] = state;

            Compilation? compilation = await project.GetCompilationAsync();

            if (compilation is null)
            {
                throw new InvalidOperationException($"Failed to get compilation for project '{project.Name}'.");
            }

            List<ISourceGenerator> generators = new List<ISourceGenerator>();
            foreach (AnalyzerReference analyzerRef in project.AnalyzerReferences)
            {
                ImmutableArray<ISourceGenerator> genArray = analyzerRef.GetGenerators(project.Language);
                generators.Capacity += genArray.Length;

                foreach (ISourceGenerator gen in genArray)
                {
                    generators.Add(gen);
                }
            }

            state.Generators = generators;
            state.InitialCompilation = compilation;

            List<AdditionalText> additional = new List<AdditionalText>(project.AdditionalDocuments.Count());
            foreach (TextDocument textDocument in project.AdditionalDocuments)
            {
                if (textDocument.FilePath is null) continue;

                additional.Add(new FileAdditionalText(textDocument.FilePath));
            }

            state.AdditionalTexts = ImmutableArray.CreateRange(additional);

            state.Driver = CSharpGeneratorDriver.Create(
                state.Generators,
                state.AdditionalTexts,
                (CSharpParseOptions)project.ParseOptions!,
                project.AnalyzerOptions.AnalyzerConfigOptionsProvider);

            state.MetadataRefCount = project.MetadataReferences.Count;
            state.AnalyzerRefCount = project.AnalyzerReferences.Count;
            state.AdditionalDocCount = project.AdditionalDocuments.Length();

            state.Driver = state.Driver!
                .WithUpdatedParseOptions((CSharpParseOptions)project.ParseOptions!)
                .WithUpdatedAnalyzerConfigOptions(project.AnalyzerOptions.AnalyzerConfigOptionsProvider);

            state.Driver = state.Driver!.RunGeneratorsAndUpdateCompilation(
                state.InitialCompilation!,
                out Compilation updatedCompilation,
                out _);

            state.InitialCompilation = updatedCompilation;

            List<SyntaxTree> generatedFilesToRemove = new List<SyntaxTree>();
            foreach (SyntaxTree tree in state.InitialCompilation.SyntaxTrees)
            {
                bool found = false;
                foreach (Document document in project.Documents)
                {
                    if (!string.Equals(document.FilePath, tree.FilePath))
                    {
                        continue;
                    }

                    found = true;
                    break;
                }

                if (!found)
                {
                    generatedFilesToRemove.Add(tree);
                }
            }

            state.InitialCompilation = state.InitialCompilation.RemoveSyntaxTrees(generatedFilesToRemove);
            state.TreesByPath = new Dictionary<string, SyntaxTree>(project.Documents.Count(), StringComparer.OrdinalIgnoreCase);
            
            state.ParseOptions = (CSharpParseOptions)project.ParseOptions!;
            state.AnalyzerOptions = project.AnalyzerOptions.AnalyzerConfigOptionsProvider;

            foreach (Document document in project.Documents)
            {
                SyntaxTree? tree = await document.GetSyntaxTreeAsync();

                if (tree is null)
                {
                    throw new Exception($"Failed to get syntax tree for document '{document.Name}' in project '{project.Name}'.");
                }
                
                state.TreesByPath[document.FilePath!] = tree;
            }

            LogUnrealSharpEditor.Log($"Project '{project.Name}' loaded for incremental generation.");
        }
        catch (Exception exception)
        {
            LogUnrealSharpEditor.LogError($"Failed to process project '{project.Name}': {exception}");
        }
    }
    
    public static async void AddProjectAsync(string projectPath, IntPtr callbackPtr)
    {
        try
        {
            await AddProjectAsync_Internal(projectPath);
        
            unsafe
            {
                delegate* unmanaged[Cdecl]<void> callback = (delegate* unmanaged[Cdecl]<void>)callbackPtr;
                callback();
            }
        }
        catch (Exception exception)
        {
            LogUnrealSharpEditor.LogError($"Failed to add project '{projectPath}': {exception}");
        }
    }
    
    private static async Task AddProjectAsync_Internal(string projectPath)
    {
        Project project = await UnrealSharpWorkspace.OpenProjectAsync(projectPath);
        await ProcessProject(project);
    }

    public static unsafe void GetDependentProjects(string projectName, UnmanagedArray* dependentsArray)
    {
        List<string> dependents = new List<string>();

        Project? foundProject = null;
        foreach (Project project in UnrealSharpWorkspace.CurrentSolution.Projects)
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

        foreach (Project project in UnrealSharpWorkspace.CurrentSolution.Projects)
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

    public static void DirtyFile(string projectName, string filepath)
    {
        string fullPath = Path.GetFullPath(filepath);

        Project? foundProject = null;
        foreach (Project project in UnrealSharpWorkspace.CurrentSolution.Projects)
        {
            if (!string.Equals(project.Name, projectName, StringComparison.Ordinal))
            {
                continue;
            }

            foundProject = project;
            break;
        }

        if (foundProject is null)
        {
            throw new Exception($"Project '{projectName}' not found in solution.");
        }

        if (!States.TryGetValue(foundProject.Id, out GenState? state))
        {
            throw new Exception($"Project '{projectName}' not initialized for incremental generation.");
        }

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

        if (state.TreesByPath!.TryGetValue(fullPath, out SyntaxTree? existingState))
        {
            state.InitialCompilation = state.InitialCompilation!.ReplaceSyntaxTree(existingState, newTree);
        }
        else
        {
            state.InitialCompilation = state.InitialCompilation!.AddSyntaxTrees(newTree);
        }
        
        state.TreesByPath[fullPath] = newTree;
        DirtyProjectIds.Add(foundProject.Id);
    }

    public static void ProcessDirtyProjects()
    {
        foreach (ProjectId dirtyProjectId in DirtyProjectIds)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();

            Project? project = UnrealSharpWorkspace.CurrentSolution.GetProject(dirtyProjectId);
            if (project is null)
            {
                throw new Exception($"Project with ID '{dirtyProjectId}' not found in solution.");
            }

            GenState state = States[project.Id];
            CSharpParseOptions parseOptions = (CSharpParseOptions)project.ParseOptions!;
            AnalyzerConfigOptionsProvider analyzerOptions = project.AnalyzerOptions.AnalyzerConfigOptionsProvider;
            
            bool firstTime = state.Driver == null;
            bool analyzersChanged = state.AnalyzerRefCount != project.AnalyzerReferences.Count;
            bool additionalChanged = state.AdditionalDocCount != project.AdditionalDocuments.Length();
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
                state.AdditionalDocCount = project.AdditionalDocuments.Length();
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

        List<string> assemblies = new List<string>(UnrealSharpWorkspace.CurrentSolution.Projects.Count());

        foreach (Project project in UnrealSharpWorkspace.CurrentSolution.Projects)
        {
            assemblies.Add(MakeAssemblyPath(project));
        }

        string outputDir = Path.GetDirectoryName(assemblies[0])!;
        AssemblyLoadOrder.EmitLoadOrder(assemblies, outputDir);
    }

    private static string MakePath(Project project, string extension)
    {
        string projectDir = Path.GetDirectoryName(project.FilePath!)!;
        string outputDirectory = project.OutputFilePath != null
            ? Path.GetDirectoryName(project.OutputFilePath)!
            : Path.Combine(projectDir, "bin", "Debug");

        Directory.CreateDirectory(outputDirectory);
        return Path.Combine(outputDirectory, project.AssemblyName + extension);
    }

    private static string MakeAssemblyPath(Project project)
    {
        string extension = project.OutputFilePath != null ? Path.GetExtension(project.OutputFilePath) : ".dll";
        return MakePath(project, extension);
    }
    
    private static string GetPlatformSymbolExtension()
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

        string assemblyPath = MakeAssemblyPath(project);
        string symbolsPath = MakePath(project, GetPlatformSymbolExtension());

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