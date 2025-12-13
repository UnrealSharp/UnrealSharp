using Microsoft.CodeAnalysis;
using UnrealSharp.Core;

namespace UnrealSharp.Editor.Utilities;

public static class ProjectUtilities
{
    public static List<FName> GetDependentProjectsAsFName(this Project projectToCheck, IList<Project> allProjects)
    {
        List<string> dependentProjectNames = GetDependentProjectsAsString(projectToCheck, allProjects);
        List<FName> dependentFNames = new List<FName>(dependentProjectNames.Count);
        
        foreach (string projectName in dependentProjectNames)
        {
            dependentFNames.Add(projectName);
        }
        
        return dependentFNames;
    }
    
    public static List<string> GetDependentProjectsAsString(this Project projectToCheck, IList<Project> allProjects)
    {
        List<Project> dependentProjects = GetDependentProjects(projectToCheck, allProjects);
        List<string> dependentProjectNames = new List<string>(dependentProjects.Count);
        
        foreach (Project project in dependentProjects)
        {
            dependentProjectNames.Add(project.Name);
        }

        return dependentProjectNames;
    }
    
    public static List<Project> GetDependentProjects(this Project projectToCheck, IList<Project> allProjects)
    {
        List<Project> dependentProjects = new List<Project>();
        
        foreach (Project project in allProjects)
        {
            if (project == projectToCheck)
            {
                continue;
            }
                    
            foreach (ProjectReference projectReference in project.ProjectReferences)
            {
                if (projectReference.ProjectId != projectToCheck.Id)
                {
                    continue;
                }
                    
                dependentProjects.Add(project);
                break;
            }
        }

        return dependentProjects;
    }
    
    public static List<Project> GetProjectsFromNames(List<string> projectNames, IList<Project> allProjects)
    {
        List<Project> projects = new List<Project>();
        
        foreach (string projectName in projectNames)
        {
            foreach (Project project in allProjects)
            {
                if (project.Name != projectName)
                {
                    continue;
                }
                
                projects.Add(project);
                break;
            }
        }

        return projects;
    }
}