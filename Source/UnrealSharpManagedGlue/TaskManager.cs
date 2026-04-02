using System.Collections.Generic;
using System.Threading.Tasks;
using EpicGames.UHT.Utils;
using UnrealSharpManagedGlue.Utilities;

namespace UnrealSharpManagedGlue;

public static class TaskManager
{
	private static readonly List<Task> Tasks = new();

	public static void StartTask(UhtExportTaskDelegate action)
	{
		Task? task = GeneratorStatics.Factory.CreateTask(action);
		
		if (task == null)
		{
			// Task execution was done synchronously
			return;
		}
		
		Tasks.Add(task);
	}
	
	public static void WaitForTasks()
	{
		Task.WaitAll(Tasks.ToArray());
	}
}