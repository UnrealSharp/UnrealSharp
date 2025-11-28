using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.CodeAnalysis.Text;
using UnrealSharp.Core;
using UnrealSharp.Editor.Utilities;
using UnrealSharp.UnrealSharpEditor;

namespace UnrealSharp.Editor;

public sealed class FileAdditionalText : AdditionalText
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

public sealed class GenState
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

public static class SolutionManager
{
    public static readonly MSBuildWorkspace UnrealSharpWorkspace = MSBuildWorkspace.Create();
    public static IList<Project> CurrentProjects => UnrealSharpWorkspace.CurrentSolution.Projects.ToList();
    private static readonly ConcurrentDictionary<ProjectId, GenState> States = new();

    public static async void LoadSolutionAsync(string solutionPath, IntPtr callbackPtr)
    {
        try
        {
            string fullSolutionPath = Path.GetFullPath(solutionPath);
            await UnrealSharpWorkspace.OpenSolutionAsync(fullSolutionPath);
            
            IList<Project> projects = UnrealSharpWorkspace.CurrentSolution.Projects.ToList();
        
            foreach (Project project in projects)
            {
                await ProcessProject(project);
            }
            
            BuildProjectDependencyMap(projects);

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
    
    private static void BuildProjectDependencyMap(IList<Project> projects)
    {
        foreach (Project project in projects)
        {
            List<FName> dependentProjects = project.GetDependentProjectsAsFName(projects);
            UCSEditorBlueprintFunctionLibrary.AddAssemblyDependencies(project.Name, dependentProjects);
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
            state.AdditionalDocCount = project.AdditionalDocuments.Count();

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
    
    public static Project? GetProjectByName(string projectName)
    {
        foreach (Project project in UnrealSharpWorkspace.CurrentSolution.Projects)
        {
            if (project.Name == projectName)
            {
                return project;
            }
        }

        return null;
    }
    
    public static GenState? GetProjectState(ProjectId projectId)
    {
        States.TryGetValue(projectId, out GenState? state);
        return state;
    }
}