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
using UnrealSharp.Editor.Utilities;
using UnrealSharpBuildTool.Actions;

namespace UnrealSharp.Editor;

public static class IncrementalCompilationManager
{
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
        
        SyntaxUtilities.LookForChangesInUTypes(newTree, existingTree, foundProject);
        
        state.TreesByPath[fullPath] = newTree;
        
        stopwatch.Stop();
        LogUnrealSharpEditor.Log($"Processed dirty file '{Path.GetFileName(filepath)}' in project '{projectName}' in {stopwatch.Elapsed.TotalMilliseconds:F2}ms.");
    }

    public static void RecompileDirtyProjects(List<string> modifiedAssemblyNames)
    {
        List<Project> projects = ProjectUtilities.GetProjectsFromNames(modifiedAssemblyNames, SolutionManager.CurrentProjects);
        
        for (int i = projects.Count - 1; i >= 0; i--)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            Project project = projects[i];
            
            LogUnrealSharpEditor.Log($"Starting source generation for project '{project.Name}'.");
            
            GenState? state = SolutionManager.GetProjectState(project.Id);
            
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
            
            UpdateDependentProjectsWithNewCompilation(updatedCompilation, project);

            stopwatch.Stop();
            LogUnrealSharpEditor.Log($"Project '{project.Name}' generated source in {stopwatch.Elapsed.TotalSeconds:F2} seconds.");
            EmitResultsToDisk(project, updatedCompilation);
        }

        List<string> assemblies = new List<string>(SolutionManager.UnrealSharpWorkspace.CurrentSolution.Projects.Count());

        foreach (Project project in SolutionManager.UnrealSharpWorkspace.CurrentSolution.Projects)
        {
            assemblies.Add(GetAssemblyOutputPath(project));
        }

        string outputDir = Path.GetDirectoryName(assemblies[0])!;
        AssemblyLoadOrder.EmitLoadOrder(assemblies, outputDir);
    }
    
    private static void UpdateDependentProjectsWithNewCompilation(Compilation newCompilation, Project producedProject)
    {
        IEnumerable<Project> dependentProjects = producedProject.GetDependentProjects(SolutionManager.CurrentProjects);
        
        foreach (Project dependentProject in dependentProjects)
        {
            GenState? projectState = SolutionManager.GetProjectState(dependentProject.Id);
            
            if (projectState is null)
            {
                throw new Exception($"Project '{dependentProject.Name}' not initialized for incremental generation.");
            }
            
            if (projectState.InitialCompilation is null)
            {
                throw new Exception($"Project '{dependentProject.Name}' has no initial compilation.");
            }
            
            Compilation depCompilation = projectState.InitialCompilation;

            MetadataReference? oldCompilationReference = null;
            foreach (MetadataReference reference in depCompilation.References)
            {
                if (reference is not CompilationReference compilationReference)
                {
                    continue;
                }

                if (compilationReference.Compilation.AssemblyName != newCompilation.AssemblyName)
                {
                    continue;
                }
                
                oldCompilationReference = reference;
                break;
            }

            CompilationReference newCompilationReference = newCompilation.ToMetadataReference();
            
            if (oldCompilationReference != null)
            {
                depCompilation = depCompilation.ReplaceReference(oldCompilationReference, newCompilationReference);
            }
            else
            {
                depCompilation = depCompilation.AddReferences(newCompilationReference);
            }
            
            projectState.InitialCompilation = depCompilation;
        }
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
        LogUnrealSharpEditor.Log($"Project '{project.Name}' produced an assembly in {stopwatch.Elapsed.TotalSeconds:F2} seconds.");
    }
}